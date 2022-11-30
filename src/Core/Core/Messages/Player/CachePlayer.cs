using System;

namespace LSG.Core.Messages.Player;

public class CachePlayer
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }

    public string NickName { get; set; }
    public string Name { get; set; }
    public string ExternalId { get; set; }
    public string CultureCode { get; set; }
    public Guid DefaultWalletId { get; set; }
    public string DefaultWalletCurrencyCode { get; set; }
    public int Level { get; set; }
}