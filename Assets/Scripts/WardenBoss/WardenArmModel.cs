using UnityEngine;

// Shows one Warden arm's state on its GLB arm node (armNode, e.g.
// "warden_arm_a"): Idle sweeps slowly, Locked draws the aim line, Firing
// flickers the muzzle flashes, Down slumps the turret, goes dark and lights
// its 12 reboot ticks as the arm recovers. Lives beside WardenArm and
// WardenArmHealth on the arm's GameObject, and only reads them.
public class WardenArmModel : MonoBehaviour
{
    public const int RebootTickCount = 12;
    private const float IdleSweepDegrees = 25.8f;
    private const float IdleSweepSpeed = 0.9f;

    public BossModel model;
    public string armNode = "warden_arm_a";
    public float sweepOffset;

    private WardenArm arm;
    private WardenArmHealth health;
    private WardenArmLook shownLook = (WardenArmLook)(-1);
    private string[] rebootTicks;

    void Awake()
    {
        arm = GetComponent<WardenArm>();
        health = GetComponent<WardenArmHealth>();
        rebootTicks = new string[RebootTickCount];
        for (int i = 0; i < RebootTickCount; i++) rebootTicks[i] = armNode + "_reboot_" + i.ToString("00");
    }

    void LateUpdate()
    {
        WardenArmLook look = LookFor(health.IsDown, arm.IsTelegraphing, arm.IsFiring);
        if (look != shownLook) ShowLook(look);
        Aim(look);
        if (look == WardenArmLook.Down) ShowRebootTicks(RebootTicksLit(health.DownRemaining, health.downDuration));
    }

    public static WardenArmLook LookFor(bool isDown, bool isTelegraphing, bool isFiring)
    {
        if (isDown) return WardenArmLook.Down;
        if (isTelegraphing) return WardenArmLook.Locked;
        return isFiring ? WardenArmLook.Firing : WardenArmLook.Idle;
    }

    /// <summary>One tick lights per twelfth of the down time, the first one
    /// as soon as the arm goes down, so all 12 are lit just before it
    /// recovers.</summary>
    public static int RebootTicksLit(float downRemaining, float downDuration)
    {
        float step = downDuration / RebootTickCount;
        int lit = Mathf.FloorToInt((downDuration - downRemaining) / step) + 1;
        return Mathf.Clamp(lit, 0, RebootTickCount);
    }

    private void ShowLook(WardenArmLook look)
    {
        shownLook = look;
        bool down = look == WardenArmLook.Down;
        model.Show(armNode + "_aim_line", look == WardenArmLook.Locked);
        model.Show(armNode + "_muzzle_flash_0", look == WardenArmLook.Firing);
        model.Show(armNode + "_muzzle_flash_1", look == WardenArmLook.Firing);
        model.Show(armNode + "_reboot", down);
        model.Show(armNode + "_sparks", down);
        GlowFor(look);
        Slump(down);
    }

    private void GlowFor(WardenArmLook look)
    {
        if (look == WardenArmLook.Idle) { ResetGlow(); return; }
        bool down = look == WardenArmLook.Down;
        model.Glow(armNode + "_lamp", down ? PaletteColor.WardenSocket : PaletteColor.WardenPale, down ? 0.3f : 3.2f);
        model.Glow(armNode + "_glow", down ? PaletteColor.WardenSocket : PaletteColor.WardenIce, down ? 0.25f : 2.2f);
        model.Glow(armNode + "_edge", down ? PaletteColor.WardenSocket : PaletteColor.WardenIce, 1f);
    }

    private void ResetGlow()
    {
        model.ResetGlow(armNode + "_lamp");
        model.ResetGlow(armNode + "_glow");
        model.ResetGlow(armNode + "_edge");
    }

    private void Slump(bool down)
    {
        Transform turret = model.Node(armNode + "_turret");
        turret.localRotation = down ? Quaternion.Euler(0f, 0f, -12.6f) : Quaternion.identity;
        turret.localPosition = down ? new Vector3(0f, -0.3f, 0f) : Vector3.zero;
        model.Node(armNode + "_barrels").localRotation = down ? Quaternion.Euler(18.3f, 0f, 0f) : Quaternion.identity;
    }

    private void Aim(WardenArmLook look)
    {
        Transform node = model.Node(armNode);
        GameObject target = arm.CurrentTarget;
        if (look == WardenArmLook.Idle) node.localRotation = Quaternion.Euler(0f, IdleSweep(), 0f);
        else if (look != WardenArmLook.Down && target != null) node.localRotation = YawToward(node, target.transform.position);
    }

    private float IdleSweep() => Mathf.Sin(Time.time * IdleSweepSpeed + sweepOffset) * IdleSweepDegrees;

    /// <summary>The barrels point along the node's local +Z at yaw 0, so the
    /// yaw is the angle of the target in the node's parent XZ plane.</summary>
    private static Quaternion YawToward(Transform node, Vector3 worldTarget)
    {
        Vector3 toTarget = node.parent.InverseTransformPoint(worldTarget) - node.localPosition;
        return Quaternion.Euler(0f, Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg, 0f);
    }

    private void ShowRebootTicks(int lit)
    {
        for (int i = 0; i < RebootTickCount; i++) model.Show(rebootTicks[i], i < lit);
    }
}
