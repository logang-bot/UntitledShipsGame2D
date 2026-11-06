// How one level-up card is drawn (design: HUD Upgrade Card): waiting, under
// focus, the one just picked, input-locked while the screen opens, or faded
// while another card's pick confirms.
public enum UpgradeCardState
{
    Rest,
    Focus,
    Picked,
    Locked,
    Dim
}
