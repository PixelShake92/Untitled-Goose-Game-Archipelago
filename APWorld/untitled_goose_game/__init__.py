from typing import Dict, Any, ClassVar
from worlds.AutoWorld import World, WebWorld
from BaseClasses import Item, Tutorial
from .Items import item_table, GooseGameItem, ITEM_GROUPS
from .Locations import location_table, GooseGameLocation, get_all_location_ids
from .Regions import create_regions
from .Options import GooseGameOptions


class GooseGameWeb(WebWorld):
    theme = "grass"
    
    setup_en = Tutorial(
        "Multiworld Setup Guide",
        "A guide to setting up Untitled Goose Game for Archipelago multiworld.",
        "English",
        "setup_en.md",
        "setup/en",
        ["YourName"]
    )
    
    tutorials = [setup_en]


class GooseGameWorld(World):
    """
    Untitled Goose Game - It's a lovely morning in the village, 
    and you are a horrible goose.
    
    Start at the Hub (well) and collect area access items to explore!
    Complete to-do list goals, pick up items, and drag objects to send checks!
    """
    
    game = "Untitled Goose Game"
    web = GooseGameWeb()
    
    options_dataclass = GooseGameOptions
    options: GooseGameOptions
    
    item_name_to_id: ClassVar[Dict[str, int]] = {
        name: data.id for name, data in item_table.items()
    }
    
    # Register ALL possible locations - AP needs these upfront or it breaks badly (Lookin at you early MM Dev Builds)
    location_name_to_id: ClassVar[Dict[str, int]] = get_all_location_ids()
    
    item_name_groups = ITEM_GROUPS
    
    def create_item(self, name: str) -> Item:
        item_data = item_table[name]
        return GooseGameItem(name, item_data.classification, item_data.id, self.player)
    
    def create_regions(self) -> None:
        create_regions(self)
    
    def get_starting_area_name(self) -> str:
        """Determine which area the player starts with access to."""
        starting_option = self.options.starting_area.value
        
        # Only 4 valid starting areas (Model Village excluded - it's the finale!)
        area_names = [
            "Garden Access",
            "High Street Access", 
            "Back Gardens Access",
            "Pub Access"
        ]
        
        if starting_option == 4:  # Random
            return self.random.choice(area_names)
        else:
            return area_names[starting_option]
    
    def create_items(self) -> None:
        # Determine starting area
        starting_area = self.get_starting_area_name()
        
        # All area access items (5 total)
        area_items = [
            "Garden Access",
            "High Street Access",
            "Back Gardens Access", 
            "Pub Access",
            "Model Village Access"
        ]
        
        # NPC Soul items (10 total) - required for NPC-related goals
        npc_souls = [
            "Groundskeeper Soul",
            "Boy Soul",
            "TV Shop Owner Soul",
            "Market Lady Soul",
            "Tidy Neighbour Soul",
            "Messy Neighbour Soul",
            "Burly Man Soul",
            "Old Man Soul",
            "Pub Lady Soul",
            "Fancy Ladies Soul",
        ]
        
        # Prop Soul items - required for picking up/dragging items
        # NOTE: NPC-tied items (Keys, Gardener Hat, Boy's Glasses, Slipper, Wooly Hat, Pub Cloth)
        # have been removed - they spawn with their NPCs and don't need separate souls
        # Vase Piece Soul also removed - pieces spawn when vase is broken
        prop_souls = [
            # Grouped Prop Souls (24 - removed Vase Piece and Slipper)
            "Carrot Soul",
            "Tomato Soul",
            "Pumpkin Soul",
            "Topsoil Bag Soul",
            "Quoit Soul",
            "Plate Soul",
            "Orange Soul",
            "Leek Soul",
            "Cucumber Soul",
            "Dart Soul",
            "Umbrella Soul",
            "Tinned Food Soul",
            "Sock Soul",
            "Pint Bottle Soul",
            "Knife Soul",
            "Gumboot Soul",
            "Fork Soul",
            # REMOVED: "Vase Piece Soul" - pieces spawn when vase is broken
            "Apple Core Soul",
            "Apple Soul",
            "Sandwich Soul",
            # REMOVED: "Slipper Soul" - slippers spawn with neighbors
            "Bow Soul",
            "Walkie Talkie Soul",
            "Boot Soul",
            "Mini Person Soul",
            
            # Garden Prop Souls (18 - removed Keys and Gardener Hat)
            "Radio Soul",
            "Trowel Soul",
            # REMOVED: "Keys Soul" - keys spawn with Groundskeeper
            "Tulip Soul",
            "Jam Soul",
            "Picnic Mug Soul",
            "Thermos Soul",
            "Straw Hat Soul",
            "Drink Can Soul",
            "Tennis Ball Soul",
            # REMOVED: "Gardener Hat Soul" - hat spawns with Groundskeeper
            "Rake Soul",
            "Picnic Basket Soul",
            "Esky Soul",
            "Shovel Soul",
            "Watering Can Soul",
            "Fence Bolt Soul",
            "Mallet Soul",
            "Wooden Crate Soul",
            "Gardener Sign Soul",
            
            # High Street Prop Souls (23 - removed Boy's Glasses, moved Boards to Back Gardens)
            # REMOVED: "Boy's Glasses Soul" - glasses spawn with Boy
            "Horn-Rimmed Glasses Soul",
            "Red Glasses Soul",
            "Sunglasses Soul",
            "Loo Paper Soul",
            "Toy Car Soul",
            "Hairbrush Soul",
            "Toothbrush Soul",
            "Stereoscope Soul",
            "Dish Soap Bottle Soul",
            "Spray Bottle Soul",
            "Weed Tool Soul",
            "Lily Flower Soul",
            "Fusilage Soul",
            "Chalk Soul",
            "Dustbin Lid Soul",
            "Shopping Basket Soul",
            "Push Broom Soul",
            "Broken Broom Head Soul",
            "Dustbin Soul",
            "Baby Doll Soul",
            "Pricing Gun Soul",
            "Adding Machine Soul",
            # MOVED: Boards Soul to Back Gardens
            
            # Back Gardens Prop Souls (26 - added Boards)
            "Boards Soul",  # MOVED from High Street - boards are at entrance to Back Gardens
            "Dummy Soul",
            "Cricket Ball Soul",
            "Bust Pipe Soul",
            "Bust Hat Soul",
            "Bust Glasses Soul",
            "Tea Cup Soul",
            "Newspaper Soul",
            "Badminton Racket Soul",
            "Pot Stack Soul",
            "Soap Soul",
            "Paintbrush Soul",
            "Vase Soul",
            "Right Strap Soul",
            "Rose Soul",
            "Rose Box Soul",
            "Cricket Bat Soul",
            "Tea Pot Soul",
            "Clippers Soul",
            "Duck Statue Soul",
            "Frog Statue Soul",
            "Jeremy Fish Soul",
            "Messy Sign Soul",
            "Drawer Soul",
            "Enamel Jug Soul",
            "Clean Sign Soul",
            
            # Pub Prop Souls (20 - removed Wooly Hat and Pub Cloth)
            "Fishing Bobber Soul",
            "Exit Letter Soul",
            "Pint Glass Soul",
            "Toy Boat Soul",
            # REMOVED: "Wooly Hat Soul" - hat spawns with Old Man
            "Pepper Grinder Soul",
            # REMOVED: "Pub Cloth Soul" - cloth spawns with Pub Lady
            "Cork Soul",
            "Candlestick Soul",
            "Flower for Vase Soul",
            "Harmonica Soul",
            "Tackle Box Soul",
            "Traffic Cone Soul",
            "Exit Parcel Soul",
            "Stealth Box Soul",
            "No Goose Sign Soul",
            "Portable Stool Soul",
            "Dartboard Soul",
            "Mop Bucket Soul",
            "Mop Soul",
            "Delivery Box Soul",
            "Burly Mans Bucket Soul",
            
            # Model Village Prop Souls (11)
            "Mini Mail Pillar Soul",
            "Mini Phone Door Soul",
            "Mini Shovel Soul",
            "Poppy Flower Soul",
            "Timber Handle Soul",
            "Birdbath Soul",
            "Easel Soul",
            "Mini Bench Soul",
            "Mini Pump Soul",
            "Mini Street Bench Soul",
            "Sun Lounge Soul",
            
            # Victory item soul - required to spawn/pick up the Golden Bell
            "Golden Bell Soul",
        ]
        
        # Add area items to pool (except the starting one)
        for item_name in area_items:
            if item_name == starting_area:
                # Give starting area to player directly (precollected)
                self.multiworld.push_precollected(self.create_item(item_name))
            else:
                self.multiworld.itempool.append(self.create_item(item_name))
        
        # Track items added for filler calculation
        items_added = 4  # 4 area items in pool (1 is precollected)
        
        # Add NPC souls to pool if option enabled
        if self.options.include_npc_souls:
            for soul in npc_souls:
                self.multiworld.itempool.append(self.create_item(soul))
            items_added += len(npc_souls)
        
        # Add Prop souls to pool if option enabled
        if self.options.include_prop_souls:
            for soul in prop_souls:
                self.multiworld.itempool.append(self.create_item(soul))
            items_added += len(prop_souls)
        
        # NOTE: Golden Bell is NOT added to the pool here!
        # It is placed directly at "Pick up Golden Bell" location in pre_fill()
        # This ensures players must have Golden Bell Soul to access it
        
        # Calculate filler needed
        total_locations = len(self.multiworld.get_unfilled_locations(self.player))
        filler_needed = total_locations - items_added

        # Add Silent Steps first since there can only be one
        if self.options.filler_active_silent_steps:
            self.multiworld.itempool.append(self.create_item("Silent Steps"))
            filler_needed -= 1

        # Add other capped fillers next
        for i in range (self.options.filler_amount_mega_honk.value): # Max 3
            self.multiworld.itempool.append(self.create_item("Mega Honk"))
            filler_needed -= 1
        for i in range (self.options.filler_amount_speedy_feet.value): # Max 10
            self.multiworld.itempool.append(self.create_item("Speedy Feet"))
            filler_needed -= 1
        for i in range (self.options.filler_amount_goose_day.value): # Max 3
            self.multiworld.itempool.append(self.create_item("A Goose Day"))
            filler_needed -= 1
        
        # Remaining filler items based on weights
        if filler_needed > 0:
            # Build weighted filler pool from options
            # Format: (item_name, weight)
            weighted_items = []
            coins_weight = self.options.filler_weight_coins.value
            tired_goose_weight = self.options.trap_weight_tired_goose.value
            confused_feet_weight = self.options.trap_weight_confused_feet.value
            butterbeak_weight = self.options.trap_weight_butterbeak.value
            suspicious_goose_weight = self.options.trap_weight_suspicious_goose.value
            
            # Filler items
            if coins_weight > 0:
                weighted_items.append(("Coin", coins_weight))
            
            # Trap items
            if tired_goose_weight > 0:
                weighted_items.append(("Tired Goose", tired_goose_weight))
            if confused_feet_weight > 0:
                weighted_items.append(("Confused Feet", confused_feet_weight))
            if butterbeak_weight > 0:
                weighted_items.append(("Butterbeak", butterbeak_weight))
            if suspicious_goose_weight > 0:
                weighted_items.append(("Suspicious Goose", suspicious_goose_weight))
                
            total_weight = 0
            for item_name, weight in weighted_items:
                total_weight += weight
            
            for _ in range(filler_needed):
                if total_weight == 0:
                    # All weighted filler has been turned off, so Coins are forced
                    item_name = "Coin"
                else:
                    # Weighted random selection
                    roll = self.random.randint(1, total_weight)
                    if roll <= coins_weight:
                        item_name = "Coin"
                    elif roll <= coins_weight + tired_goose_weight:
                        item_name = "Tired Goose"
                    elif roll <= coins_weight + tired_goose_weight + confused_feet_weight:
                        item_name = "Confused Feet"
                    elif roll <= coins_weight + tired_goose_weight + confused_feet_weight + butterbeak_weight:
                        item_name = "Butterbeak"
                    elif roll <= coins_weight + tired_goose_weight + confused_feet_weight + butterbeak_weight + suspicious_goose_weight:
                        item_name = "Suspicious Goose"
                    else:
                        item_name = "Coin" # We shouldn't reach this, but if something goes wrong, we fall back on Coins
                
                self.multiworld.itempool.append(self.create_item(item_name))
    
    def pre_fill(self) -> None:
        """Place victory-related items at their fixed locations.
        
        The Golden Bell is placed directly at 'Pick up Golden Bell' rather than
        being in the item pool. This ensures players must:
        1. Have Pub Access (to reach Model Village area)
        2. Have Model Village Access (to enter Model Village)
        3. Have Golden Bell Soul (to spawn/pick up the bell)
        
        Milestone items are placed based on goal option:
        - Goal 1 (All Main Goals): "All Main Goals Complete" at milestone location
        - Goal 2 (All Goals): "All Goals Complete" at milestone location
        """
        golden_bell = self.create_item("Golden Bell")
        golden_bell_location = self.multiworld.get_location("Pick up Golden Bell", self.player)
        golden_bell_location.place_locked_item(golden_bell)
        
        # Place milestone items based on goal option
        goal = self.options.goal.value
        if goal == 1:  # All Main Goals
            milestone_item = self.create_item("All Main Goals Complete")
            milestone_location = self.multiworld.get_location("All Main Task Lists Complete", self.player)
            milestone_location.place_locked_item(milestone_item)
        elif goal == 2:  # All Goals
            milestone_item = self.create_item("All Goals Complete")
            milestone_location = self.multiworld.get_location("All Tasks Complete", self.player)
            milestone_location.place_locked_item(milestone_item)
    
    def set_rules(self) -> None:
        from .Rules import set_rules
        set_rules(self)
    
    def fill_slot_data(self) -> Dict[str, Any]:
        return {
            "starting_area": self.get_starting_area_name(),
            "include_extra_goals": self.options.include_extra_goals.value,
            "include_speedrun_goals": self.options.include_speedrun_goals.value,
            "include_item_pickups": self.options.include_item_pickups.value,
            "include_drag_items": self.options.include_drag_items.value,
            "include_interactions": self.options.include_interactions.value,
            "include_npc_souls": self.options.include_npc_souls.value,
            "include_prop_souls": self.options.include_prop_souls.value,
            "goal": self.options.goal.value,
            "death_link": self.options.death_link.value,
        }