//https://github.com/Sofoca/CoinUtils

using Xunit;
using Xunit.Abstractions;

namespace WalletAddressValidator;

public class AddressValidatorTest
{
    private readonly ITestOutputHelper _output;

    public AddressValidatorTest(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string[] _invalidBech32Addresses = {
        "tc1qw508d6qejxtdg4y5r3zarvary0c5xw7kg3g4ty",
        "bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t5",
        "BC13W508D6QEJXTDG4Y5R3ZARVARY0C5XW7KN40WF2",
        "bc1rw5uspcuh",
        "bc10w508d6qejxtdg4y5r3zarvary0c5xw7kw508d6qejxtdg4y5r3zarvary0c5xw7kw5rljs90",
        "BC1QR508D6QEJXTDG4Y5R3ZARVARYV98GJ9P",
        "tb1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3q0sL5k7",
        "tb1pw508d6qejxtdg4y5r3zarqfsj6c3",
        "tb1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3pjxtptv",
        "bc1pw508d6qeJxtdg4y5r3zarvary0c5xw7kw508d6qejxtdg4y5r3zarvary0c5xw7k7grplx"
    };

    private static string[][] _validBech32AddressWithScriptPubKey = {
        new [] { "BC1QW508D6QEJXTDG4Y5R3ZARVARY0C5XW7KV8F3T4", "0014751e76e8199196d454941c45d1b3a323f1433bd6"},
        new [] { "bc1pw508d6qejxtdg4y5r3zarvary0c5xw7kw508d6qejxtdg4y5r3zarvary0c5xw7k7grplx", "5128751e76e8199196d454941c45d1b3a323f1433bd6751e76e8199196d454941c45d1b3a323f1433bd6"},
        new [] { "BC1SW50QA3JX3S", "6002751e"},
        new [] { "bc1zw508d6qejxtdg4y5r3zarvaryvg6kdaj", "5210751e76e8199196d454941c45d1b3a323"}
    };

    private static string[] _customValidBtcAddresses = {
        "bc1q8y3tcxx0kv3jlmqmcrgp43v88sdmndtalk6m0u",
        "1KrdVUYd7gyFjuseQqGkdqMBwUEFR4LYJV",
    };

    private static string[] _customInvalidBtcAddresses = {
        "bc1q8y3tcxx0kv3jlmqmcrgp43v88sdmmdtalk6m0u",
        "1KrdVUYd7gyFjuseQqGkdqMBwUEFR4LYJW",
        "0x799BC33d125b20089E3A851966508fe912FA8c25",
        "LM476W7gaVJvNEP73rEwR4iyXVsifSkh7H",
    };

    private static string[] _customValidEthAddresses = {
        "0x799BC33d125b20089E3A851966508fe912FA8c25",
        "0xbd31ea8212119f94a611fa969881cba3ea06fa3d",
    };
    
    private static string[] _customValidTupAddresses = {
        "0x0742520ac2554989d5484773f0e4029ad6b1b4c7",
    };
    
    private static string[] _customInvalidEthAddresses = {
        "0x799BC33d125b20089E3A851966508fe912FA8c26",
    };

    [Fact]
    public void Custom_Btc_AddressValidation()
    {
        foreach (var address in _customValidBtcAddresses)
        {
            Assert.True(AddressValidator.IsValidBtcAddress(address));
        }
        
        foreach (var address in _customInvalidBtcAddresses)
        {
            Assert.False(AddressValidator.IsValidBtcAddress(address));
        }
    }
    
    [Fact]
    public void Custom_Eth_AddressValidation()
    {
        foreach (var address in _customValidEthAddresses)
        {
            Assert.True(AddressValidator.IsValidEthAddress(address));
        }
        
        foreach (var address in _customValidTupAddresses)
        {
            Assert.True(AddressValidator.IsValidEthAddress(address));
        }

        foreach (var address in _customInvalidEthAddresses)
        {
            Assert.False(AddressValidator.IsValidEthAddress(address));
        }
    }

    [Fact]
    public void EmptyBtcAddress()
    {
        Assert.False(AddressValidator.IsValidBtcAddress(""));
    }
    [Fact]
    public void nullBtcAddress()
    {
        Assert.False(AddressValidator.IsValidBtcAddress(null));
    }

    [Fact]
    public void GoodBtcAddress()
    {
        Assert.True(AddressValidator.IsValidBtcAddress("1comboyNsev2ubWRbPZpxxNhghLfonzuN"));
    }

    [Fact]
    public void GoodBtcAddressesBech32()
    {
        foreach (var address in _validBech32AddressWithScriptPubKey)
        {
            Assert.True(AddressValidator.IsValidBtcAddress(address[0]));
        }
    }

    [Fact]
    public void BadBtcAddressesBech32()
    {
        foreach (var address in _invalidBech32Addresses)
        {
            _output.WriteLine(address);
            Assert.False(AddressValidator.IsValidBtcAddress(address));
        }

    }

    [Fact]
    public void GoodBtcAddressP2SH()
    {
        Assert.True(AddressValidator.IsValidBtcAddress("3J98t1WpEZ73CNmQviecrnyiWrnqRhWNLy"));
    }

    [Fact]
    public void GoodBtcAddressP2PKH()
    {
        Assert.True(AddressValidator.IsValidBtcAddress("1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2"));
    }

    [Fact]
    public void BadBtcAddressActuallyLitecoinLegacy()
    {
        Assert.False(AddressValidator.IsValidBtcAddress("LgADTx6JrydCVdrrhJ8wkFkXdx3UszKsFx"));
    }

    [Fact]
    public void BadBtcAddressActuallyLitecoinSegwitLegacy()
    {
        Assert.True(AddressValidator.IsValidBtcAddress("3JEE5m1NaUCKCTXwyPkRhwiKzJUaKJDsJi"));
    }

    [Fact]
    public void BadBtcAddressActuallyLitecoinSegwitNew()
    {
        Assert.False(AddressValidator.IsValidBtcAddress("MAP2uc4aFVwJwoJp3p8yFMs7zy6Pa5e9Zv"));
    }
}
