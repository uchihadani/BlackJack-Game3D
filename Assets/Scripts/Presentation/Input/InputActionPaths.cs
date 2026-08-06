namespace TwentyThree.Presentation.Input
{
    public static class InputActionPaths
    {
        public const string GlobalMap = "Global";
        public const string ExplorationMap = "Exploration";
        public const string TableMap = "Table";
        public const string UiMap = "UI";

        public const string Pause = GlobalMap + "/Pause";
        public const string Move = ExplorationMap + "/Move";
        public const string Look = ExplorationMap + "/Look";
        public const string Interact = ExplorationMap + "/Interact";
        public const string LeaveTable = TableMap + "/LeaveTable";
    }
}
