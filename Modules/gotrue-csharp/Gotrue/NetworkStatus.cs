using System.Collections.Generic;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Supabase.Gotrue.Interfaces;
namespace Supabase.Gotrue
{
	/// <summary>
	/// A Network status system to pair with the <see cref="Client.Online"/>Client.
	/// 
	/// <see>
	///     <cref>https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/network-info</cref>
	/// </see>
	/// </summary>
	public class NetworkStatus
	{
		private readonly List<NetworkListener> _listeners = new List<NetworkListener>();

		/// <summary>
		/// True if the network has been checked.
		/// </summary>
		public bool Ready;

		/// <summary>
		/// A delegate for listening to network changes.
		/// </summary>
		public delegate void NetworkListener(bool isNetworkAvailable);

		/// <summary>
		/// Adds a listener to the network status system.
		/// </summary>
		/// <param name="listener"></param>
		public void AddListener(NetworkListener listener)
		{
			_listeners.Add(listener);
		}

		/// <summary>
		/// Removes a listener from the network status system.
		/// </summary>
		/// <param name="listener"></param>
		public void RemoveListener(NetworkListener listener)
		{
			_listeners.Remove(listener);
		}

		private void NotifyListeners(bool isNetworkAvailable)
		{
			foreach (NetworkListener listener in _listeners)
			{
				listener?.Invoke(isNetworkAvailable);
			}
		}

		/// <summary>
		/// The <see cref="Client"/> that this network status system is attached to.
		/// </summary>
		public IGotrueClient<User, Session>? Client { get; set; }

		/// <summary>
		/// Pings the URL in the <see cref="Client.Options"/> to check if the network is online.
		/// 
		/// https://PROJECTID.supabase.co/auth/v1/settings
		/// </summary>
		public async Task<bool> PingCheck(string url)
		{
			try
			{
				var reply = await new HttpClient().GetAsync(url);
				if (reply?.StatusCode == System.Net.HttpStatusCode.OK)
				{
					UpdateStatus(true);
					return true;
				}
				UpdateStatus(false);
			}
			catch (HttpRequestException e)
			{
				Client?.Debug($"Network Problem: {e.Message}");
				UpdateStatus(false);
			}
			catch (SocketException e)
			{
				Client?.Debug($"Network Problem: {e.Message}");
				UpdateStatus(false);
			}
			catch (PingException e)
			{
				Client?.Debug($"Network Problem: {e.Message}");
				UpdateStatus(false);
			}
			return false;
		}

		private void UpdateStatus(bool isNetworkAvailable)
		{
			Ready = true;
			NotifyListeners(isNetworkAvailable);
			if (Client != null)
				Client.Online = isNetworkAvailable;
		}

		/// <summary>
		/// Starts the network status system. This will listen to the OS for network changes,
		/// and also does a ping check to confirm the current network status.
		/// </summary>
		public async Task<bool> StartAsync(string url)
		{
			NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
			return await PingCheck(url);
		}

		private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			UpdateStatus(e.IsAvailable);
		}

		/// <summary>
		/// Removes the network status system checker from the OS.
		/// </summary>
		~NetworkStatus()
		{
			NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
		}
	}

}
