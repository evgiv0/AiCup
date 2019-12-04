using AiCup2019.Model;

internal class Target
{
    public Vec2Double Position{ get; set; }
    public Purpose Purpose { get; set; }
    public bool SwapWeapon { get; set; }

    public bool NeedJump { get; set; }
}