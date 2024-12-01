[System.Serializable]
public class LevelData {
    public float timeRatio;  
    public Level[] levels;

    [System.Serializable]
    public class Level {
        public Point[] path;  // Path coordinates
        public Event[] level;   // List of events

        [System.Serializable]
        public class Point {
            public int x;
            public int z;
        }

        [System.Serializable]
        public class Event {
            public int eventType;
            public int eventLength;
            public int startKey;
            public int endKey;
        }
    }
}
