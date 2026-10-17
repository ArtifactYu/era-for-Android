namespace MinorShift.Emuera.Api;

// EraFlow modification: hide game images only at final presentation. Sprite creation
// and game logic keep running, so toggling visibility never changes script results.
public static class DisplayOptions
{
    public static bool ShowImages { get; set; } = true;
}
