namespace LSG.Core.Enums
{
    public enum ApiResponseCode
    {
        Success = 0,

        Forbidden = 403,
        NotFound = 404,
        NotSupport = 415,
        SystemError = 500,

        ConfiguredTimeoutExceeded = 800,

        //Internal

        MissingToken = 101400,
        IncorrectFormat = 102400,
        InvalidModelBinding = 103400,
        NotSupportCurrency = 104400,
        ExpiredOrUnauthorizedToken = 101401,
        InvalidCredentials = 102401,
        UserCacheNotExist = 103401,
        LiveClientCacheExpired = 104401,
        IgnoredRout = 101404,
        DataNotExist = 102404,
        InvalidArguments = 101500,
        AnchorOffline = 101503,


        //Fish
        InvalidLiveStreamToken = 201401,
        ExpiredLiveStreamToken = 202401,
        PlayerCanNotSendGift = 203401,
        NotSupportTestPlayer = 204401,
        InvalidGameResponse = 201500,
        MaxBossOrAnchorFishComeOut = 202500,
        InvalidTimeForBossFish = 203500,
        SendGiftInsufficientFund = 204500,
        LastAnchorFishNotFinished = 205500,


        //ChatRoom
        PlayerCanNotUseMegaPhone = 205401,
    }
}