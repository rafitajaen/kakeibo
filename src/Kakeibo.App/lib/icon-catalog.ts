// Curated catalog of Lucide icons for wallet customization.
// Each entry maps a display name to the Lucide icon component name.
// Icons are imported dynamically in IconPicker.vue.

export interface IconEntry {
    name: string; // Lucide component name (e.g. "Wallet")
    label: string; // Human-readable label for search/tooltip
}

export interface IconGroup {
    group: string;
    icons: IconEntry[];
}

export const ICON_GROUPS: IconGroup[] = [
    {
        group: "Finance",
        icons: [
            { name: "Wallet", label: "Wallet" },
            { name: "PiggyBank", label: "Piggy Bank" },
            { name: "Banknote", label: "Banknote" },
            { name: "CreditCard", label: "Credit Card" },
            { name: "Coins", label: "Coins" },
            { name: "DollarSign", label: "Dollar Sign" },
            { name: "Euro", label: "Euro" },
            { name: "TrendingUp", label: "Trending Up" },
            { name: "TrendingDown", label: "Trending Down" },
            { name: "BarChart3", label: "Bar Chart" },
            { name: "LineChart", label: "Line Chart" },
            { name: "Receipt", label: "Receipt" },
            { name: "Landmark", label: "Bank" },
            { name: "BadgeDollarSign", label: "Dollar Badge" },
            { name: "ArrowUpDown", label: "Transfer" },
        ],
    },
    {
        group: "Home & Living",
        icons: [
            { name: "Home", label: "Home" },
            { name: "Building2", label: "Building" },
            { name: "Sofa", label: "Sofa" },
            { name: "Utensils", label: "Utensils" },
            { name: "Zap", label: "Electricity" },
            { name: "Droplets", label: "Water" },
            { name: "Flame", label: "Gas" },
            { name: "Wifi", label: "Internet" },
            { name: "Phone", label: "Phone" },
            { name: "ShoppingCart", label: "Shopping Cart" },
            { name: "Package", label: "Package" },
            { name: "Wrench", label: "Maintenance" },
        ],
    },
    {
        group: "Transport",
        icons: [
            { name: "Car", label: "Car" },
            { name: "Bus", label: "Bus" },
            { name: "Train", label: "Train" },
            { name: "Plane", label: "Plane" },
            { name: "Bike", label: "Bike" },
            { name: "Ship", label: "Ship" },
            { name: "Fuel", label: "Fuel" },
            { name: "MapPin", label: "Location" },
            { name: "Navigation", label: "Navigation" },
        ],
    },
    {
        group: "Health & Sport",
        icons: [
            { name: "Heart", label: "Health" },
            { name: "Activity", label: "Activity" },
            { name: "Stethoscope", label: "Doctor" },
            { name: "Pill", label: "Medicine" },
            { name: "Dumbbell", label: "Gym" },
            { name: "PersonStanding", label: "Person" },
            { name: "Apple", label: "Nutrition" },
            { name: "Smile", label: "Wellness" },
        ],
    },
    {
        group: "Entertainment",
        icons: [
            { name: "Tv", label: "TV" },
            { name: "Music", label: "Music" },
            { name: "Film", label: "Cinema" },
            { name: "Gamepad2", label: "Gaming" },
            { name: "BookOpen", label: "Books" },
            { name: "Camera", label: "Photography" },
            { name: "Star", label: "Star" },
            { name: "PartyPopper", label: "Events" },
            { name: "Coffee", label: "Coffee" },
            { name: "Wine", label: "Dining" },
            { name: "Cake", label: "Celebration" },
        ],
    },
    {
        group: "Work & Education",
        icons: [
            { name: "Briefcase", label: "Business" },
            { name: "GraduationCap", label: "Education" },
            { name: "Laptop", label: "Laptop" },
            { name: "Monitor", label: "Computer" },
            { name: "Pen", label: "Writing" },
            { name: "Paperclip", label: "Documents" },
            { name: "FolderOpen", label: "Files" },
            { name: "Globe", label: "Online" },
            { name: "Tool", label: "Tools" },
        ],
    },
    {
        group: "General",
        icons: [
            { name: "Target", label: "Target" },
            { name: "Repeat", label: "Recurring" },
            { name: "Gift", label: "Gift" },
            { name: "ShieldAlert", label: "Emergency" },
            { name: "Leaf", label: "Nature" },
            { name: "Sun", label: "Sun" },
            { name: "Moon", label: "Moon" },
            { name: "Tag", label: "Tag" },
            { name: "Layers", label: "Layers" },
            { name: "Box", label: "Box" },
            { name: "Archive", label: "Archive" },
            { name: "Sparkles", label: "Sparkles" },
        ],
    },
];

// Flat list of all icons for search
export const ALL_ICONS: IconEntry[] = ICON_GROUPS.flatMap((g) => g.icons);
