using System.Threading.Tasks;
using LSG.Core.Messages.Hub;

namespace LSG.Core.Interfaces;

public interface INotifier
{
    Task NotifyUserExpiredAsync(UserExpiredMessage message);
}