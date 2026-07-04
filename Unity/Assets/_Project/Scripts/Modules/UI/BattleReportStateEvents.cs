using System;

namespace Project.Modules.Reports
{
    public static class BattleReportStateEvents
    {
        public static event Action UnreadStateChanged;

        public static void RaiseUnreadStateChanged()
        {
            UnreadStateChanged?.Invoke();
        }
    }
}
