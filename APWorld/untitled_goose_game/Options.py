from dataclasses import dataclass
from Options import Toggle, Range, Choice, PerGameCommonOptions


class StartingArea(Choice):
    """Which area the player starts with access to. 
    Without this, you'd be stuck at the hub with nothing to do!
    Model Village is not an option since it's the finale area."""
    display_name = "Starting Area"
    option_garden = 0
    option_high_street = 1
    option_back_gardens = 2
    option_pub = 3
    option_random = 4
    default = 4  # Random by default


class IncludeExtraGoals(Toggle):
    """Include post-game extra challenge goals as locations."""
    display_name = "Include Extra Goals"
    default = False


class IncludeSpeedrunGoals(Toggle):
    """Include speedrun challenge goals (complete areas before noon)."""
    display_name = "Include Speedrun Goals"
    default = False


class IncludeItemPickups(Toggle):
    """Include first-time item pickups as locations (100+ additional checks)."""
    display_name = "Include Item Pickups"
    default = True


class IncludeDragItems(Toggle):
    """Include first-time drag item locations (50+ additional checks for heavy/draggable items)."""
    display_name = "Include Drag Items"
    default = True


class IncludeInteractions(Toggle):
    """Include interaction locations (ringing bells, spinning windmills, breaking boards, etc.)."""
    display_name = "Include Interactions"
    default = True


class IncludeNPCSouls(Toggle):
    """When enabled, NPCs won't appear until you receive their soul item.
    This adds NPC souls to the item pool and gates NPC-related goals behind them."""
    display_name = "Include NPC Souls"
    default = True


class IncludePropSouls(Toggle):
    """When enabled, items can't be picked up or dragged until you receive their soul.
    This adds prop souls to the item pool and gates item interactions behind them."""
    display_name = "Include Prop Souls"
    default = True


class FillerAmountMegaHonk(Range):
    """Amount of Mega Honk in the filler pool.
    Mega Honk effects - upgraded honking abilities
    Level 1: All NPCs react to honk (draws attention) - default behavior enhanced
    Level 2: Increased honk detection distance - always heard regardless of distance
    Level 3: Scary honk - NPCs drop held items"""
    display_name = "Filler Amount: Mega Honk"
    range_start = 0
    range_end = 3
    default = 3


class FillerAmountSpeedyFeet(Range):
    """Amount of Speedy Feet in filler pool. Speedy Feet increases your run speed."""
    display_name = "Filler Amount: Speedy Feet"
    range_start = 0
    range_end = 10
    default = 10


class FillerActiveSilentSteps(Toggle):
    """Whether or not Silent Steps is in the filler pool. Silent Steps prevents NPCs from hearing your footsteps."""
    display_name = "Filler Active: Silent Steps"
    default = True


class FillerAmountGooseDay(Range):
    """Amount of A Goose Day in filler pool. A Goose Day causes NPCs to ignore you for 15 seconds."""
    display_name = "Filler Amount: A Goose Day"
    range_start = 0
    range_end = 3
    default = 3

class FillerWeightCoins(Range):
    """Weight for Coins in the filler pool. Set to 0 to disable this filler.
    Please note that if you turn off all traps below, this filler item will be chosen to fill all remaining spots regardless of chosen weight."""
    display_name = "Filler Weight: Coins"
    range_start = 0
    range_end = 100
    default = 80


class TrapWeightTiredGoose(Range):
    """Weight for Tired Goose trap in the filler pool. Set to 0 to disable this trap."""
    display_name = "Trap Weight: Tired Goose"
    range_start = 0
    range_end = 100
    default = 5


class TrapWeightConfusedFeet(Range):
    """Weight for Confused Feet trap in the filler pool. Set to 0 to disable this trap."""
    display_name = "Trap Weight: Confused Feet"
    range_start = 0
    range_end = 100
    default = 5


class TrapWeightButterbeak(Range):
    """Weight for Butterbeak trap in the filler pool. Set to 0 to disable this trap."""
    display_name = "Trap Weight: Butterbeak"
    range_start = 0
    range_end = 100
    default = 5


class TrapWeightSuspiciousGoose(Range):
    """Weight for Suspicious Goose trap in the filler pool. Set to 0 to disable this trap."""
    display_name = "Trap Weight: Suspicious Goose"
    range_start = 0
    range_end = 100
    default = 5


class Goal(Choice):
    """What is required to complete the game."""
    display_name = "Goal"
    option_steal_bell = 0
    option_all_main_goals = 1
    option_all_goals = 2
    default = 0


class DeathLink(Toggle):
    """When you get caught/shooed, everyone dies. When someone else dies, you get teleported to the hub."""
    display_name = "Death Link"
    default = False


@dataclass
class GooseGameOptions(PerGameCommonOptions):
    starting_area: StartingArea
    include_extra_goals: IncludeExtraGoals
    include_speedrun_goals: IncludeSpeedrunGoals
    include_item_pickups: IncludeItemPickups
    include_drag_items: IncludeDragItems
    include_interactions: IncludeInteractions
    include_npc_souls: IncludeNPCSouls
    include_prop_souls: IncludePropSouls
    filler_amount_mega_honk: FillerAmountMegaHonk
    filler_amount_speedy_feet: FillerAmountSpeedyFeet
    filler_active_silent_steps: FillerActiveSilentSteps
    filler_amount_goose_day: FillerAmountGooseDay
    filler_weight_coins: FillerWeightCoins
    trap_weight_tired_goose: TrapWeightTiredGoose
    trap_weight_confused_feet: TrapWeightConfusedFeet
    trap_weight_butterbeak: TrapWeightButterbeak
    trap_weight_suspicious_goose: TrapWeightSuspiciousGoose
    goal: Goal
    death_link: DeathLink