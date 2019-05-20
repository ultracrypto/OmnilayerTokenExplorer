using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinLib
{
    public class LibConfig
    {
        public static short RpcRequestTimeoutInSeconds { get; set; } = 60;

        public static string Bitcoin_DaemonUrl { get; set; } = "http://localhost:8332";
        public static string Bitcoin_DaemonUrl_Testnet { get; set; } = "http://localhost:18332";
        public static string Bitcoin_WalletPassword { get; set; } = "MyWalletPassword";
        public static string Bitcoin_RpcUsername { get; set; } = "MyRpcUsername";
        public static string Bitcoin_RpcPassword { get; set; } = "MyRpcPassword";   

        public static string Litecoin_DaemonUrl { get; set; } = "http://localhost:9332";
        public static string Litecoin_DaemonUrl_Testnet { get; set; } = "http://localhost:19332";
        public static string Litecoin_WalletPassword { get; set; } = "MyWalletPassword";
        public static string Litecoin_RpcUsername { get; set; } = "MyRpcUsername";
        public static string Litecoin_RpcPassword { get; set; } = "MyRpcPassword";

        public static string Dogecoin_DaemonUrl { get; set; } = "http://localhost:22555";
        public static string Dogecoin_DaemonUrl_Testnet { get; set; } = "http://localhost:44555";
        public static string Dogecoin_WalletPassword { get; set; } = "MyWalletPassword";
        public static string Dogecoin_RpcUsername { get; set; } = "MyRpcUsername";
        public static string Dogecoin_RpcPassword { get; set; } = "MyRpcPassword";

        public static string Sarcoin_DaemonUrl { get; set; } = "http://localhost:25901";
        public static string Sarcoin_DaemonUrl_Testnet { get; set; } = "http://localhost:36523";
        public static string Sarcoin_WalletPassword { get; set; } = "MyWalletPassword";
        public static string Sarcoin_RpcUsername { get; set; } = "MyRpcUsername";
        public static string Sarcoin_RpcPassword { get; set; } = "MyRpcPassword";

        public static string Dash_DaemonUrl { get; set; } = "http://localhost:9998";
        public static string Dash_DaemonUrl_Testnet { get; set; } = "http://localhost:19998";
        public static string Dash_WalletPassword { get; set; } = "MyWalletPassword";
        public static string Dash_RpcUsername { get; set; } = "MyRpcUsername";
        public static string Dash_RpcPassword { get; set; } = "MyRpcPassword";

        public static bool ExtractMyPrivateKeys { get; set; } = false;
    }
}
