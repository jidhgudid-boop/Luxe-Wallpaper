namespace LuxeWallpaper.Services
{
    /// <summary>
    /// PowerSavingService 单例管理器
    /// 用于在设置窗口和主视图模型之间共享同一个 PowerSavingService 实例
    /// </summary>
    public static class PowerSavingServiceInstance
    {
        private static PowerSavingService? _instance;
        private static readonly object _lock = new object();

        public static PowerSavingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PowerSavingService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 设置外部创建的实例（用于主ViewModel初始化时）
        /// </summary>
        public static void SetInstance(PowerSavingService service)
        {
            lock (_lock)
            {
                _instance = service;
            }
        }
    }
}
