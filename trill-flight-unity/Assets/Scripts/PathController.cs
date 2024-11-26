using UnityEngine;

[System.Serializable]
public class HoopPlacement
{
    public float time;   // Proportion of the total path length (0 to 1)
    public float height; // Height (y-coordinate) of the hoop
    public bool type;
}
public class PathController : MonoBehaviour
{
    public Transform[] pathPoints; // Waypoints for the predefined path
    public HoopPlacement[] hoopPlacements; // Array of time and height data for hoops
    public GameObject hoopPrefab; // The torus prefab (hoop)
    public GameObject barPrefab;
    public float movementSpeed = 5f; // Speed along the path

    private float t = 0f; // Parameter for interpolation
    private int segmentIndex = 0; // Current segment of the spline

    void Start()
    {
        if (pathPoints.Length < 4)
        {
            Debug.LogError("At least 4 path points are required for Catmull-Rom interpolation.");
            return;
        }

        if (hoopPrefab == null)
        {
            Debug.LogError("Please assign the hoop prefab in the Inspector.");
            return;
        }

        if (barPrefab == null)
        {
            Debug.LogError("Please assign the bar prefab in the Inspector.");
            return;
        }

        GenerateHoops();
        GenerateBars();
    }

    void Update()
    {
        if (pathPoints.Length < 4) return; // Need at least 4 points for Catmull-Rom

        // Move along the spline
        t += (movementSpeed / Vector3.Distance(pathPoints[segmentIndex + 1].position, pathPoints[segmentIndex + 2].position)) * Time.deltaTime;
        if (t >= 1f)
        {
            t = 0f;
            segmentIndex++;

            // Loop back to the start if at the end of the path
            if (segmentIndex >= pathPoints.Length - 3)
            {
                segmentIndex = 0;
            }
        }

        // Interpolate position using Catmull-Rom spline
        Vector3 positionOnPath = CatmullRom(
            pathPoints[segmentIndex].position,
            pathPoints[segmentIndex + 1].position,
            pathPoints[segmentIndex + 2].position,
            pathPoints[segmentIndex + 3].position,
            t
        );

        // Preserve the y-axis while updating x and z
        transform.position = new Vector3(positionOnPath.x, transform.position.y, positionOnPath.z);

        // Calculate forward direction along the spline
        Vector3 forwardDirection = CatmullRom(
            pathPoints[segmentIndex].position,
            pathPoints[segmentIndex + 1].position,
            pathPoints[segmentIndex + 2].position,
            pathPoints[segmentIndex + 3].position,
            t + 0.01f // A small offset to calculate the forward direction
        ) - positionOnPath;

        if (forwardDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(forwardDirection, Vector3.up);
        }
    }
    void GenerateBars()
    {
        foreach (var placement in hoopPlacements) {
            if (placement.type == true) continue;
            float totalPathLength = CalculateTotalPathLength();
            float targetDistance = placement.time * totalPathLength;

            Vector3 hoopPosition = GetPositionOnPathAtDistance(targetDistance);
            Vector3 forwardDirection = GetForwardOnPathAtDistance(targetDistance);

            // Place the hoop
            GameObject hoop = Instantiate(barPrefab, hoopPosition, Quaternion.identity);
            hoop.transform.position = new Vector3(hoopPosition.x, placement.height, hoopPosition.z);

            // Rotate the hoop to face the forward direction
            Quaternion rotation = Quaternion.LookRotation(forwardDirection, Vector3.up);
            Quaternion additionalRotation = Quaternion.Euler(0f, 0f, 90f); // Additional X-axis rotation
            hoop.transform.rotation = rotation * additionalRotation; // Combine the rotations
        }
    }

    void GenerateHoops()
    {
        foreach (var placement in hoopPlacements) {
            if (placement.type == false) continue;
            float totalPathLength = CalculateTotalPathLength();
            float targetDistance = placement.time * totalPathLength;

            Vector3 hoopPosition = GetPositionOnPathAtDistance(targetDistance);
            Vector3 forwardDirection = GetForwardOnPathAtDistance(targetDistance);

            // Place the hoop
            GameObject hoop = Instantiate(hoopPrefab, hoopPosition, Quaternion.identity);
            hoop.transform.position = new Vector3(hoopPosition.x, placement.height, hoopPosition.z);

            // Rotate the hoop to face the forward direction
            Quaternion rotation = Quaternion.LookRotation(forwardDirection, Vector3.up);
            Quaternion additionalRotation = Quaternion.Euler(90f, 0f, 0f); // Additional X-axis rotation
            hoop.transform.rotation = rotation * additionalRotation; // Combine the rotations
        }
    }

    Vector3 GetPositionOnPathAtDistance(float distance)
    {
        float accumulatedDistance = 0f;

        for (int i = 0; i < pathPoints.Length - 3; i++)
        {
            Vector3 start = pathPoints[i].position;
            Vector3 end = pathPoints[i + 1].position;
            float segmentLength = Vector3.Distance(start, end);

            if (accumulatedDistance + segmentLength >= distance)
            {
                float t = (distance - accumulatedDistance) / segmentLength;
                return CatmullRom(
                    pathPoints[i].position,
                    pathPoints[i + 1].position,
                    pathPoints[i + 2].position,
                    pathPoints[i + 3].position,
                    t
                );
            }

            accumulatedDistance += segmentLength;
        }

        return pathPoints[pathPoints.Length - 1].position; // Return the last point if distance exceeds total length
    }

    Vector3 GetForwardOnPathAtDistance(float distance)
    {
        float accumulatedDistance = 0f;

        for (int i = 0; i < pathPoints.Length - 3; i++)
        {
            Vector3 start = pathPoints[i].position;
            Vector3 end = pathPoints[i + 1].position;
            float segmentLength = Vector3.Distance(start, end);

            if (accumulatedDistance + segmentLength >= distance)
            {
                float t = (distance - accumulatedDistance) / segmentLength;
                Vector3 position = CatmullRom(
                    pathPoints[i].position,
                    pathPoints[i + 1].position,
                    pathPoints[i + 2].position,
                    pathPoints[i + 3].position,
                    t
                );
                Vector3 nextPosition = CatmullRom(
                    pathPoints[i].position,
                    pathPoints[i + 1].position,
                    pathPoints[i + 2].position,
                    pathPoints[i + 3].position,
                    t + 0.01f
                );
                return (nextPosition - position).normalized;
            }

            accumulatedDistance += segmentLength;
        }

        return Vector3.forward; // Default direction if distance exceeds total length
    }

    float CalculateTotalPathLength()
    {
        float totalLength = 0f;

        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            totalLength += Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);
        }

        return totalLength;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}
