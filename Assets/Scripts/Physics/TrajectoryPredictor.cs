using UnityEngine;

namespace Siege.Physics
{
    /// <summary>
    /// Predicts the trajectory of projectiles for ghost arc visualization.
    /// Uses discrete time step simulation to match the actual physics engine.
    /// </summary>
    public static class TrajectoryPredictor
    {
        private const float TIME_STEP = 0.05f;
        private const float MAX_PREDICTION_TIME = 5f;
        private const int MAX_POINTS = 100;
        
        /// <summary>
        /// Predicts the full trajectory of a projectile.
        /// </summary>
        /// <param name="startPosition">Starting position of the projectile</param>
        /// <param name="velocity">Initial velocity magnitude</param>
        /// <param name="direction">Launch direction (normalized)</param>
        /// <param name="environment">Environmental variables affecting trajectory</param>
        /// <returns>Array of points representing the trajectory</returns>
        public static Vector3[] Predict(Vector3 startPosition, float velocity, Vector3 direction, Level.EnvironmentVariables environment)
        {
            if (environment == null)
            {
                environment = new Level.EnvironmentVariables();
            }
            
            List<Vector3> points = new List<Vector3>();
            Vector3 position = startPosition;
            Vector3 velocityVector = direction * velocity;
            
            // Apply initial wind effect
            velocityVector += ApplyWind(environment, TIME_STEP);
            
            float time = 0f;
            int pointCount = 0;
            
            while (time < MAX_PREDICTION_TIME && pointCount < MAX_POINTS)
            {
                points.Add(position);
                pointCount++;
                
                // Apply gravity
                velocityVector += Physics2D.gravity * TIME_STEP;
                
                // Apply wind
                velocityVector += ApplyWind(environment, TIME_STEP);
                
                // Update position
                position += velocityVector * TIME_STEP;
                
                // Check ground collision
                if (position.y < 0f)
                {
                    // Interpolate to find exact ground intersection
                    Vector3 previousPoint = points[points.Count - 1];
                    float t = (0f - previousPoint.y) / (position.y - previousPoint.y);
                    Vector3 groundPoint = Vector3.Lerp(previousPoint, position, t);
                    points.Add(groundPoint);
                    break;
                }
                
                time += TIME_STEP;
            }
            
            return points.ToArray();
        }
        
        /// <summary>
        /// Predicts the impact point of a projectile.
        /// </summary>
        /// <param name="startPosition">Starting position of the projectile</param>
        /// <param name="velocity">Initial velocity magnitude</param>
        /// <param name="direction">Launch direction (normalized)</param>
        /// <param name="environment">Environmental variables affecting trajectory</param>
        /// <returns>The predicted impact point</returns>
        public static Vector3 PredictImpactPoint(Vector3 startPosition, float velocity, Vector3 direction, Level.EnvironmentVariables environment)
        {
            Vector3[] points = Predict(startPosition, velocity, direction, environment);
            return points.Length > 0 ? points[points.Length - 1] : startPosition;
        }
        
        /// <summary>
        /// Predicts the trajectory with simplified physics (for accessibility mode).
        /// </summary>
        public static Vector3[] PredictSimplified(Vector3 startPosition, float velocity, Vector3 direction, Level.EnvironmentVariables environment)
        {
            // Simplified physics: no wind, reduced gravity effect
            Level.EnvironmentVariables simplifiedEnv = new Level.EnvironmentVariables
            {
                windDirection = Vector2.zero,
                windStrength = 0f,
                elevationModifier = environment?.elevationModifier ?? 0f,
                obstacles = environment?.obstacles
            };
            
            return Predict(startPosition, velocity * 1.1f, direction, simplifiedEnv);
        }
        
        /// <summary>
        /// Calculates the launch velocity needed to hit a target.
        /// </summary>
        /// <param name="startPosition">Starting position</param>
        /// <param name="targetPosition">Target position</param>
        /// <param name="angle">Launch angle in degrees</param>
        /// <param name="environment">Environmental variables</param>
        /// <returns>The required launch velocity, or -1 if impossible</returns>
        public static float CalculateRequiredVelocity(Vector3 startPosition, Vector3 targetPosition, float angle, Level.EnvironmentVariables environment)
        {
            Vector3 displacement = targetPosition - startPosition;
            float horizontalDistance = displacement.x;
            float verticalDistance = displacement.y;
            
            // Convert angle to radians
            float angleRad = angle * Mathf.Deg2Rad;
            
            // Calculate required velocity using projectile motion formula
            // v = sqrt(g * d^2 / (2 * cos^2(theta) * (d * tan(theta) - h)))
            
            float g = Physics2D.gravity.y;
            float cosTheta = Mathf.Cos(angleRad);
            float sinTheta = Mathf.Sin(angleRad);
            
            float denominator = 2f * cosTheta * cosTheta * (horizontalDistance * Mathf.Tan(angleRad) - verticalDistance);
            
            if (denominator <= 0f)
            {
                return -1f; // Impossible to hit target with this angle
            }
            
            float velocitySquared = -g * horizontalDistance * horizontalDistance / denominator;
            
            if (velocitySquared < 0f)
            {
                return -1f;
            }
            
            float velocity = Mathf.Sqrt(velocitySquared);
            
            // Add wind compensation
            if (environment != null && environment.windStrength > 0f)
            {
                velocity += environment.windStrength * 0.5f;
            }
            
            return velocity;
        }
        
        /// <summary>
        /// Gets the optimal launch angle for a given velocity and target.
        /// </summary>
        /// <param name="startPosition">Starting position</param>
        /// <param name="targetPosition">Target position</param>
        /// <param name="velocity">Launch velocity</param>
        /// <param name="environment">Environmental variables</param>
        /// <returns>The optimal angle in degrees, or -1 if impossible</returns>
        public static float CalculateOptimalAngle(Vector3 startPosition, Vector3 targetPosition, float velocity, Level.EnvironmentVariables environment)
        {
            Vector3 displacement = targetPosition - startPosition;
            float horizontalDistance = displacement.x;
            float verticalDistance = displacement.y;
            
            float g = Physics2D.gravity.y;
            float velocitySquared = velocity * velocity;
            
            // Calculate discriminant for quadratic equation
            float discriminant = velocitySquared * velocitySquared - g * (g * horizontalDistance * horizontalDistance - 2f * verticalDistance * velocitySquared);
            
            if (discriminant < 0f)
            {
                return -1f; // Impossible to hit target with this velocity
            }
            
            // Calculate two possible angles
            float angle1 = Mathf.Atan((velocitySquared - Mathf.Sqrt(discriminant)) / (g * horizontalDistance));
            float angle2 = Mathf.Atan((velocitySquared + Mathf.Sqrt(discriminant)) / (g * horizontalDistance));
            
            // Return the higher angle (more arc) for better visibility
            float optimalAngle = Mathf.Max(angle1, angle2) * Mathf.Rad2Deg;
            
            // Clamp to valid range
            optimalAngle = Mathf.Clamp(optimalAngle, 0f, 90f);
            
            return optimalAngle;
        }
        
        /// <summary>
        /// Checks if a trajectory will hit a specific target.
        /// </summary>
        /// <param name="startPosition">Starting position</param>
        /// <param name="velocity">Launch velocity</param>
        /// <param name="direction">Launch direction</param>
        /// <param name="targetPosition">Target position</param>
        /// <param name="targetRadius">Radius of the target</param>
        /// <param name="environment">Environmental variables</param>
        /// <returns>True if the trajectory will hit the target</returns>
        public static bool WillHitTarget(Vector3 startPosition, float velocity, Vector3 direction, Vector3 targetPosition, float targetRadius, Level.EnvironmentVariables environment)
        {
            Vector3[] trajectory = Predict(startPosition, velocity, direction, environment);
            
            foreach (Vector3 point in trajectory)
            {
                float distance = Vector3.Distance(point, targetPosition);
                if (distance <= targetRadius)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Applies wind effect to velocity.
        /// </summary>
        private static Vector3 ApplyWind(Level.EnvironmentVariables environment, float deltaTime)
        {
            if (environment == null || environment.windStrength <= 0f)
            {
                return Vector3.zero;
            }
            
            // Wind affects horizontal velocity
            Vector2 windForce = environment.windDirection * environment.windStrength * 0.1f;
            return windForce * deltaTime;
        }
        
        /// <summary>
        /// Gets the maximum range for a given velocity and angle.
        /// </summary>
        public static float GetMaxRange(float velocity, float angle, Level.EnvironmentVariables environment)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            float g = Mathf.Abs(Physics2D.gravity.y);
            
            // Range formula: R = v^2 * sin(2*theta) / g
            float range = (velocity * velocity * Mathf.Sin(2f * angleRad)) / g;
            
            // Reduce range for wind
            if (environment != null && environment.windStrength > 0f)
            {
                float windFactor = 1f - (environment.windStrength * 0.1f);
                range *= windFactor;
            }
            
            return range;
        }
        
        /// <summary>
        /// Gets the maximum height for a given velocity and angle.
        /// </summary>
        public static float GetMaxHeight(float velocity, float angle)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            float g = Mathf.Abs(Physics2D.gravity.y);
            
            // Height formula: H = v^2 * sin^2(theta) / (2*g)
            float height = (velocity * velocity * Mathf.Sin(angleRad) * Mathf.Sin(angleRad)) / (2f * g);
            
            return height;
        }
        
        /// <summary>
        /// Gets the time of flight for a given velocity and angle.
        /// </summary>
        public static float GetTimeOfFlight(float velocity, float angle)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            float g = Mathf.Abs(Physics2D.gravity.y);
            
            // Time formula: T = 2 * v * sin(theta) / g
            float time = (2f * velocity * Mathf.Sin(angleRad)) / g;
            
            return time;
        }
    }
}
