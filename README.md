# Teleporting
![Discord](https://img.shields.io/discord/742861338233274418?label=Discord&logo=Discord) ![Github All Releases](https://img.shields.io/github/downloads/F-Plugins/Teleporting/total?label=Downloads) ![GitHub release (latest by date)](https://img.shields.io/github/v/release/F-Plugins/Teleporting?label=Version)

Makes teleportation possible on your server

### Download Now
RocketMod: [ClickMe](https://github.com/F-Plugins/Teleporting/releases)
OpenMod: `openmod install Feli.Teleporting`

### Commands
- tpa accept
- tpa send [playerName]
- tpa cancel
- tpa list

### Configuration
```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <MessageColor>magenta</MessageColor>
  <MessageIcon />
  <TeleportDelay>5</TeleportDelay>
  <TeleportCooldown>60</TeleportCooldown>
  <CancelWhenMove>false</CancelWhenMove>
  <TeleportProtection>true</TeleportProtection>
  <TeleportProtectionTime>5</TeleportProtectionTime>
  <TeleportCombatAllowed>false</TeleportCombatAllowed>
  <TeleportCombatTime>30</TeleportCombatTime>
  <AllowAcceptingWithKeys>true</AllowAcceptingWithKeys>
  <AutoAcceptSameGroupRequests>false</AutoAcceptSameGroupRequests>
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
  <Translation Id="TpaCommand:Send:Cooldown" Value="You cannot send a tpa request. Wait {0} seconds" />
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
  <Translation Id="TpaCommand:Send:Combat" Value="Could not send the tpa request because you are in combat. You must wait {0} seconds" />
  <Translation Id="TpaProtection" Value="You must wait {0} seconds to damage {1} he is on tpa protection" />
  <Translation Id="TpaValidation:Combat:Sender" Value="The teleport was cancelled because you are in combat. The combat mode expires in {0} seconds" />
  <Translation Id="TpaValidation:Combat:Target" Value="The teleport was cancelled because {0} is in combat" />
</Translations>
```
