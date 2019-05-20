// Copyright (c) 2014 - 2016 George Kimionis
// See the accompanying file LICENSE for the Software License Aggrement

namespace BitcoinLib.RPC.Specifications
{
    //  Note: Do not alter the capitalization of the enum members as they are being cast as-is to the RPC server
    public enum RpcMethods
    {
        //== Blockchain ==
        getbestblockhash,
        getblock,
        getblockchaininfo,
        getblockcount,
        getblockhash,
        getblockheader,
        getchaintips,
        getdifficulty,
        getmempoolinfo,
        getrawmempool,
        gettxout,
        gettxoutproof,
        gettxoutsetinfo,
        verifychain,
        verifytxoutproof,

        //== Control ==
        getinfo,
        help,
        stop,

        //== Generating ==
        generate,
        getgenerate,
        setgenerate,

        //== Mining ==
        getblocktemplate,
        getmininginfo,
        getnetworkhashps,
        prioritisetransaction,
        submitblock,

        //== Network ==
        addnode,
        clearbanned,
        disconnectnode,
        getaddednodeinfo,
        getconnectioncount,
        getnettotals,
        getnetworkinfo,
        getpeerinfo,
        listbanned,
        ping,
        setban,

        //== masternodelist ==
        masternode,
        listclabnodes,
        masternodelist,
        smartnodelist,
        servicenodelist,
        znodelist,
        listmasternodes,

        //== Rawtransactions ==
        createrawtransaction,
        decoderawtransaction,
        decodescript,
        fundrawtransaction,
        getrawtransaction,
        sendrawtransaction,
        signrawtransaction,
        sighashtype,

        //== Util ==
        createmultisig,
        estimatefee,
        estimatepriority,
        estimatesmartfee,
        estimatesmartpriority,
        validateaddress,
        verifymessage,

        //== Wallet ==
        abandontransaction,
        addmultisigaddress,
        addwitnessaddress,
        backupwallet,
        dumpprivkey,
        dumpwallet,
        getaccount,
        getaccountaddress,
        getaddressesbyaccount,
        getbalance,
        getnewaddress,
        getrawchangeaddress,
        getreceivedbyaccount,
        getreceivedbyaddress,
        gettransaction,
        getunconfirmedbalance,
        getwalletinfo,
        importaddress,
        importprivkey,
        importpubkey,
        importwallet,
        keypoolrefill,
        listaccounts,
        listaddressgroupings,
        listlockunspent,
        listreceivedbyaccount,
        listreceivedbyaddress,
        listsinceblock,
        listtransactions,
        listunspent,
        lockunspent,
        move,
        sendfrom,
        sendmany,
        sendtoaddress,
        setaccount,
        settxfee,
        signmessage,
        walletlock,
        walletpassphrase,
        walletpassphrasechange,

        //budget
        getbudgetinfo,
        getbudgetprojection,
        getbudgetvotes,
        mnbudget,

        omni_getactivations,
        omni_getactivecrowdsales,
        omni_getactivedexsells, // ( address )
        omni_getallbalancesforaddress, // "address"
        omni_getallbalancesforid, // propertyid
        omni_getbalance, // "address" propertyid
        omni_getbalanceshash, // propertyid
        omni_getcrowdsale, // propertyid ( verbose )
        omni_getcurrentconsensushash, //
        omni_getfeecache, // ( propertyid )
        omni_getfeedistribution, //distributionid
        omni_getfeedistributions, // propertyid
        omni_getfeeshare, // ( address ecosystem )
        omni_getfeetrigger, // ( propertyid )
        omni_getgrants, // propertyid
        omni_getinfo, //
        omni_getmetadexhash, // propertyId
        omni_getorderbook, // propertyid ( propertyid )
        omni_getpayload, // "txid"
        omni_getproperty, // propertyid
        omni_getseedblocks, // startblock endblock
        omni_getsto, // "txid" "recipientfilter"
        omni_gettrade, // "txid"
        omni_gettradehistoryforaddress, // "address" ( count propertyid )
        omni_gettradehistoryforpair, // propertyid propertyid ( count )
        omni_gettransaction, // "txid"
        omni_getwalletaddressbalances, // ( includewatchonly )
        omni_getwalletbalances, // ( includewatchonly )
       // omni_listblocktransactions, // firstblock lastblock
        omni_listblocktransactions, // index
        omni_listpendingtransactions, // ( "address" )
        omni_listproperties, //
        omni_listtransactions, // ( "address" count skip startblock endblock ),

    }
}