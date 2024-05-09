﻿namespace MainCore.Notification.Message
{
    public class StorageUpdated : ByAccountVillageIdBase, INotification
    {
        public StorageUpdated(AccountId accountId, VillageId villageId) : base(accountId, villageId)
        {
        }
    }
}