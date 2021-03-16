namespace GF.Core
{
    public static class AutoIncrementID
    {
        private static object ms_Lock = new object();
        private static int ms_AutoIncrementID = -1;

        public static int AutoID()
        {
            lock (ms_Lock)
            {
                ms_AutoIncrementID++;
            }

            return ms_AutoIncrementID;
        }
    }
}