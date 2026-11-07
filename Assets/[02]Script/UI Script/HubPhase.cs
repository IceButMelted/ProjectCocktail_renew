/// <summary>Phases a Hub UI can be shown in. Mirrors the flow's steps; the flow itself is not referenced.</summary>
[Flags]
public enum HubPhase { None = 0, BeforeOpen = 1, Talking = 2, AddIngredient = 4, Minigame = 8, Garnish = 16, Serve = 32, Close = 64 }
