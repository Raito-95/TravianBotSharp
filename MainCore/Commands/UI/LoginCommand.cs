namespace MainCore.Commands.UI
{
    [RegisterSingleton<LoginCommand>]
    public class LoginCommand
    {
        private readonly IChromeManager _chromeManager;
        private readonly IDialogService _dialogService;
        private readonly ITaskManager _taskManager;
        private readonly IMediator _mediator;
        private readonly ILogService _logService;
        private readonly ITimerManager _timerManager;

        public LoginCommand(IChromeManager chromeManager, IDialogService dialogService, ITaskManager taskManager, IMediator mediator, ILogService logService, ITimerManager timerManager)
        {
            _chromeManager = chromeManager;
            _dialogService = dialogService;
            _taskManager = taskManager;
            _mediator = mediator;
            _logService = logService;
            _timerManager = timerManager;
        }

        public async Task Execute(AccountId accountId, CancellationToken cancellationToken)
        {
            var getSetting = Locator.Current.GetService<IGetSetting>();
            var tribe = (TribeEnums)getSetting.ByName(accountId, AccountSettingEnums.Tribe);
            if (tribe == TribeEnums.Any)
            {
                _dialogService.ShowMessageBox("警告", "請先選擇部落");
                return;
            }

            if (_taskManager.GetStatus(accountId) != StatusEnums.Offline)
            {
                _dialogService.ShowMessageBox("警告", "帳號的瀏覽器已經打開");
                return;
            }

            await _taskManager.SetStatus(accountId, StatusEnums.Starting);
            var logger = _logService.GetLogger(accountId);

            var getAccess = Locator.Current.GetService<GetAccess>();
            var result = await getAccess.Execute(accountId, true);

            if (result.IsFailed)
            {
                _dialogService.ShowMessageBox("錯誤", result.Errors.Select(x => x.Message).First());
                var errors = result.Errors.Select(x => x.Message).ToList();
                logger.Error("{Errors}", string.Join(Environment.NewLine, errors));

                await _taskManager.SetStatus(accountId, StatusEnums.Offline);
                return;
            }
            var access = result.Value;

            logger.Information("使用代理伺服器 {Proxy} 來啟動 Chrome", access.Proxy);

            var openBrowserCommand = Locator.Current.GetService<OpenBrowserCommand>();
            result = await openBrowserCommand.Execute(accountId, access, cancellationToken);
            if (result.IsFailed)
            {
                _dialogService.ShowMessageBox("錯誤", result.Errors.Select(x => x.Message).First());
                var errors = result.Errors.Select(x => x.Message).ToList();
                logger.Error("{Errors}", string.Join(Environment.NewLine, errors));
                await _taskManager.SetStatus(accountId, StatusEnums.Offline);

                var chromeBrowser = _chromeManager.Get(accountId);
                await chromeBrowser.Close();

                return;
            }
            await _mediator.Publish(new AccountInit(accountId), CancellationToken.None);

            _timerManager.Start(accountId);
            await _taskManager.SetStatus(accountId, StatusEnums.Online);
        }
    }
}