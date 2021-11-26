# Teleporting
![Discord](https://img.shields.io/discord/742861338233274418?label=Discord&logo=Discord) ![Github All Releases](https://img.shields.io/github/downloads/F-Plugins/Teleporting/total?label=Downloads) ![GitHub release (latest by date)](https://img.shields.io/github/v/release/F-Plugins/Teleporting?label=Version)

Makes teleportation possible on your server

### Download Now
[ClickMe](https://github.com/F-Plugins/Teleporting/releases)

### Commands
- tpa accept
- tpa send [playerName]
- tpa cancel

### Configuration
```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <MessageColor>magenta</MessageColor>
  <TeleportDelay>5</TeleportDelay>
  <CancelWhenMove>false</CancelWhenMove>
  <TeleportCost>
    <Enabled>false</Enabled>
    <UseXp>true</UseXp>
    <TpaCost>10</TpaCost>
  </TeleportCost>
</Configuration>
```

### Translations
```xml
<?xml version="1.0" encoding="utf-8"?>
<Translations xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <Translation Id="TpaCommand:WrongUsage" Value="Correct command usage: /tpa &lt;accept|send|cancel&gt;" />
    <Translation Id="TpaCommand:WrongUsage:Send" Value="Correct command usage: /tpa send &lt;playerName&gt;" />
    <Translation Id="TpaCommand:WrongUsage:NotFound" Value="Player with name {0} was not found" />
    <Translation Id="TpaCommand:Send:Yourself" Value="There is no point on sending a tpa request to yourself" />
    <Translation Id="TpaCommand:Send:Target" Value="{0} has just sent you a tpa request. Use &quot;/tpa accept&quot; to accept it or &#xA;/tpa cancel&#xA; to cancel it" />
    <Translation Id="TpaCommand:Send:Sender" Value="Successfully sent a tpa request to {0}. Use &quot;/tpa cancel&quot; to cancel it" />
    <Translation Id="TpaCommand:Accept:NoRequests" Value="There are no tpa requests to accept" />
    <Translation Id="TpaCommand:Accept:Delay" Value="You will be teleported to {0} in {1} seconds" />
    <Translation Id="TpaCommand:Accept:Success" Value="Successfully accepted {0}'s tpa" />
    <Translation Id="TpaCommand:Accept:Teleported" Value="Successfully teleported to {0}" />
    <Translation Id="TpaCommand:Cancel:NotRequests" Value="There are no tpa requests to cancel" />
    <Translation Id="TpaCommand:Cancel:Other" Value="{0} has just cancelled the tpa request" />
    <Translation Id="TpaCommand:Cancel:Success" Value="Successfully canceled the tpa with {0}" />
    <Translation Id="TpaValidation:Car:Other" Value="The teleport was cancelled because {0} is on a car" />
    <Translation Id="TpaValidation:Car:Self" Value="The teleport was cancelled because you are on a car" />
    <Translation Id="TpaValidation:Leave" Value="The teleport was cancelled because {0} left the server" />
    <Translation Id="TpaValidation:Balance:Sender" Value="You dont have enough balance to teleport. Teleport cost: {0}" />
    <Translation Id="TpaValidation:Balance:Target" Value="The teleport was cancelled because {0} does not have enough balance" />
</Translations>
```