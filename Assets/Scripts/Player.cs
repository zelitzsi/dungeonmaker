﻿using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IActionQueue
{
    public enum State
    {
        Idle,
        Stun,
        Rolling,
        AttackWindup,
        Attack,
        ShootWindup,
        Shoot,
        // Below are time independent states (not dictated by frame count)
        Shadow,
        Victory
    }

    // This could be an enum, but we want to preserve null as a value.
    public class Action
    {
        public State type;
        public int frames;
        public Vector2 vector;
        public bool sticky; // Should action stay around even when frames == 0?
        public bool combo; // Count this as part of the combo?

        public override string ToString()
        {
            return "[" + type + "|" + frames + "|" + vector + "]";
        }
    }

    public GameObject swordPrefab;
    public GameObject bulletPrefab;
    public Sprite swordIcon;
    public Sprite gunIcon;
    public Sprite dodgeIcon;

    public float acceleration;

    // Rolling
    public int rollFrames; // Number of frames it takes to roll
    public float rollForce; // Force with which to roll
    public float energyPerRoll;
    public float rollGravityModifier;
    public AudioClip dodgeSound;

    // Combat
    public int attackWindupFrames;
    public int attackFrames;
    public int attackForce;
    public int energyPerAttack;
    public int energyCostPerAttackCombo;
    public float energyMovementModifier;
    public int overdrawPenaltyFrames;
    public AudioClip overdrawSound;
    public AudioClip chargeSound;

    // Sword charge attack
    public float swordChargeEnergyPerFrame;
    public int minSwordChargeFrames;
    public int maxSwordChargeFrames;

    //Gunplay
    public int shootWindupFrames; // Max number of frames to actually start attacking
    public int shootFrames; // Number of frames after attack starts before player can take another action.
    public float shootKnockback; // Force to apply when attacking
    public float energyPerShot; // Energy to consume per shot
    public float scatterAngle; // Degrees by which to randomly scatter shots
    public float attackMovementModifier; // % modifier on movement while attacking

    // Gun charge attack
    public float gunChargeEnergyPerFrame; // energy consumed per frame by charging up an attack
    public int maxGunChargeFrames; // max frames to charge

    [ReadOnly]
    public int remStateFrames; // Remaining frames to continue current state; <= 0 when in idle
    /// <summary>
    /// How many frames have passed under the current action - important for charge attacks
    /// </summary>
    [ReadOnly]
    public int currentActionFrames;
    public int combo; // Current number of actions in a row

    public int keys = 0;
    public IItem[] Items { get { return GetComponentsInChildren<IItem>(); } }
    public string[] keybinds;

    [ReadOnly]
    public Vector2 roomEntrance;

    public Action currentAction;
    public Action NextQueuedAction {  get { return actions.Count > 0 ? actions[0] : null; } }
    public Action LastQueuedAction { get { return actions.Count > 0 ? actions[actions.Count - 1] : currentAction; } }
    public List<Action> actions = new List<Action>();

    // State
    [ReadOnly]
    public bool shadow = false;

    GameObject playerPanel;
    KeyUI keyIndicator;
    //ColorCorrectionCurves colorCorrection;
    //NoiseAndGrain noiseAndGrain;

    Rigidbody2D rb2d;
    Gravity gravity;
    Animator animator;
    Energy energy;
    public ParticleSystem chargeParticles;
    public ParticleSystem dashParticles;

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        gravity = GetComponentInChildren<Gravity>();
        animator = GetComponentInChildren<Animator>();
        playerPanel = LevelEditor.main.playerPanel;
        keyIndicator = playerPanel.GetComponentInChildren<KeyUI>(true);
        energy = GetComponent<Energy>();
        energy.indicator = playerPanel.GetComponentInChildren<EnergyIndicator>(true);
        //colorCorrection = Camera.main.GetComponent<ColorCorrectionCurves>();
        //noiseAndGrain = Camera.main.GetComponent<NoiseAndGrain>();
        currentAction = new Action();
    }

    /// <summary>
    /// Called the first frame an action becomes active
    /// </summary>
    public void TriggerAction(Action action)
    {
        Quaternion rotation;
        remStateFrames = action.frames;
        int previousActionFrames = currentActionFrames;
        currentActionFrames = 0;
        if (action.combo)
            combo++;
        if (action.type == State.Idle)
            combo = 0;
        if (action.type != State.Shadow)
            shadow = false;
        switch (action.type)
        {
            case State.AttackWindup:
                gravity.dragModifier = 1;
                if (action.vector.magnitude > 0)
                    rb2d.AddForce(action.vector);
                break;

            case State.Attack:
                if (action.vector.magnitude > 0)
                {
                    rotation = Quaternion.LookRotation((combo % 3 == 1 ? Vector3.forward : Vector3.back), action.vector.normalized);
                    Sword sword = Instantiate(swordPrefab, transform.position, rotation, transform).GetComponentInChildren<Sword>();
                    sword.owner = gameObject;
                    UseEnergy(energyPerAttack + energyCostPerAttackCombo * (combo - 1));
                    if (previousActionFrames - attackWindupFrames >= minSwordChargeFrames)
                    {
                        sword.style = Sword.Style.Spin;
                        sword.damage *= 2;
                        currentAction.frames = 30;
                    }
                    else if (combo % 3 == 0)
                        sword.style = Sword.Style.Thrust;
                }
                break;

            case State.ShootWindup:
                gravity.dragModifier = 1;
                if (action.vector.magnitude > 0)
                    rb2d.AddForce(action.vector);
                break;

            case State.Shoot:
                Vector2 targetDirection = (LevelEditor.main.GetXYPlanePosition(Input.mousePosition) - (Vector2)transform.position).normalized;
                rotation = Quaternion.LookRotation(Vector3.forward, targetDirection);
                // Hacky way of doing a bell curve
                float angle = GetScatter() / 2 + GetScatter() / 2;
                Quaternion scatter = Quaternion.AngleAxis(angle, Vector3.forward);
                Bullet bullet = Instantiate(bulletPrefab, transform.position, rotation * scatter).GetComponentInChildren<Bullet>();
                bullet.friendly = true;
                bullet.owner = gameObject;
                bullet.charge = Mathf.Clamp01((previousActionFrames - shootWindupFrames) / (float)maxGunChargeFrames);
                UseEnergy(energyPerShot);
                break;

            case State.Rolling:
                AudioSource.PlayClipAtPoint(dodgeSound, transform.position);
                gravity.dragModifier = rollGravityModifier;
                if (action.vector.magnitude > 0)
                    rb2d.AddForce(action.vector);
                UseEnergy(energyPerRoll);
                dashParticles.gameObject.SetActive(true);
                break;

            case State.Shadow:
                shadow = !shadow;
                break;
            case State.Idle:
            case State.Stun:
                combo = 0;
                break;
        }
    }

    float GetScatter()
    {
        return Random.Range(-scatterAngle / 2, scatterAngle / 2);
    }

    public void Interrupt(int frames, State type)
    {
        currentAction = new Action() { type = type, frames = frames };
        TriggerAction(currentAction);
    }

    public void Interrupt(int frames)
    {
        Interrupt(frames, State.Idle);
    }

    public void InterruptAfterCurrent(int frames, State type = State.Idle)
    {
        actions.Clear();
        actions.Add(new Action() { type = type, frames = frames });
    }

    bool CanQueueActions()
    {
        return (remStateFrames <= 15 && actions.Count <= 1 && 
            energy.Current > 0 &&
            (currentAction == null || currentAction.type != State.Stun) && 
            (NextQueuedAction == null || NextQueuedAction.type != State.Stun));
    }

    /// <summary>
    /// Backswing can be cancelled in a certain frame window if there are no additional actions queued.
    /// </summary>
    bool CanCancelBackswing()
    {
        return false;
        //return (1 < remStateFrames && remStateFrames < 10 && currentAction.type != State.Idle && actions.Count == 0);
    }

    void TryCancelBackswing()
    {
        if (CanCancelBackswing())
            remStateFrames = 0;
    }

    // Consumes a certain amount of energy. Can trigger overdraw inactivity period.
    float UseEnergy(float amt)
    {
        // Don't allow player to overdraw by queueing actions
        float actual = energy.UseEnergy(amt);
        if (energy.Current == 0)
            actions.Clear();
        if (actual == 0)
            TriggerOverdraw();
        return actual;
    }

    void TriggerOverdraw()
    {
        if (overdrawSound)
            Camera.main.GetComponent<AudioSource>().PlayOneShot(overdrawSound);
        //energy.Damage(overdrawDamage, gameObject, Vector2.zero);
        InterruptAfterCurrent(overdrawPenaltyFrames, State.Stun);
    }

    Action GetFirstStickyActionOfType(State state)
    {
        if (currentAction.sticky && currentAction.type == state)
            return currentAction;
        return actions.Find((action) => { return action.sticky && action.type == state; });
    }

    void Update()
    {
        if (animator)
            animator.SetBool("shadow", shadow);
        //colorCorrection.saturation = energy.Current / energy.Limit * .65f + 0.35f;
        //noiseAndGrain.intensityMultiplier = 2 * (1 - energy.Current / energy.Limit);
    }

    void FixedUpdate()
    {
        currentActionFrames++;
        if (currentAction.type == State.Victory)
            return;

        UpdateLayer();

        if (LevelEditor.main.SetCurrentRoom(transform.position))
            roomEntrance = transform.position;

        // Handle floor
        GameObject floor = LevelEditor.main.GetGameObjectAtPointWithType(transform.position, ObjectType.Floor);
        FloorData floorData = null;
        if (floor != null)
            floorData = floor.GetComponent<FloorData>();

        //Handle movement
        float xMotion = Input.GetAxis("Horizontal");
        float yMotion = Input.GetAxis("Vertical");
        Vector2 targetMotion = Vector2.right * xMotion + Vector2.up * yMotion;
        if (currentAction.type == State.AttackWindup || currentAction.type == State.ShootWindup || currentAction.type == State.Shoot || currentAction.type == State.Idle)
        {
            float modifiedAcceleration = acceleration;
            modifiedAcceleration *= (floorData ? floorData.accelerationModifier : 1f);
            modifiedAcceleration *= Mathf.Lerp(energyMovementModifier, 1f, energy.Current / energy.max);
            modifiedAcceleration *= (currentAction.type == State.ShootWindup || currentAction.type == State.AttackWindup ? attackMovementModifier : 1f);
            rb2d.AddForce((targetMotion.magnitude > 1 ? targetMotion.normalized : targetMotion) * modifiedAcceleration);
        }

        if (remStateFrames > 0)
        {
            remStateFrames--;
            switch (currentAction.type)
            {
                case State.Rolling:
                    if (remStateFrames == 0)
                        gravity.dragModifier = 1;
                    else if (remStateFrames < rollFrames / 5)
                        gravity.dragModifier = 5;
                    break;
            }
        }
        else if (!currentAction.sticky)
        {
            chargeParticles.gameObject.SetActive(false);
            dashParticles.gameObject.SetActive(false);
            remStateFrames = 0;
            if (actions.Count > 0)
            {
                currentAction = actions[0];
                actions.RemoveAt(0);
                TriggerAction(currentAction);
            }
            else
            {
                if (currentActionFrames >= 30)
                    combo = 0;
                currentAction = new Action();
            }
        }
        else // Handle sticky actions
        {
            if (currentAction.type == State.ShootWindup)
            {
                // Must release early because the shot takes energy too, so it shouldn't always trigger overdraw.
                if (currentActionFrames - shootWindupFrames >= maxGunChargeFrames || energy.Current < energyPerShot / 2)
                {
                    currentAction.sticky = false;
                }
                else
                {
                    if (Time.frameCount % 15 == 0)
                        AudioSource.PlayClipAtPoint(chargeSound, transform.position);
                    chargeParticles.gameObject.SetActive(true);
                    UseEnergy(gunChargeEnergyPerFrame);
                }
            }
            else if (currentAction.type == State.AttackWindup)
            {
                if (currentActionFrames - attackWindupFrames >= maxSwordChargeFrames || energy.Current < energyPerAttack / 2)
                {
                    currentAction.sticky = false;
                }
                else
                {
                    if (Time.frameCount % 15 == 0)
                        AudioSource.PlayClipAtPoint(chargeSound, transform.position);
                    chargeParticles.gameObject.SetActive(true);
                    UseEnergy(swordChargeEnergyPerFrame);
                }
            }
        }

        if (Input.GetButtonUp("Shoot"))
        {
            Action firstShootWindup = GetFirstStickyActionOfType(State.ShootWindup);
            if (firstShootWindup != null)
                firstShootWindup.sticky = false;
        }
        if (currentAction != null && currentAction.type == State.ShootWindup && currentAction.sticky && !Input.GetButton("Shoot"))
            currentAction.sticky = false;

        if (Input.GetButtonUp("Attack"))
        {
            Action firstAttackWindup = GetFirstStickyActionOfType(State.AttackWindup);
            if (firstAttackWindup != null)
                firstAttackWindup.sticky = false;
        }
        if (currentAction != null && currentAction.type == State.AttackWindup && currentAction.sticky && !Input.GetButton("Attack"))
            currentAction.sticky = false;

        if (!CanQueueActions())
            return;

        if (Input.GetButtonDown("Roll") && targetMotion.magnitude > 0)
        {
            TryCancelBackswing();
            actions.Add(new Action() { type = State.Rolling, frames = rollFrames, vector = targetMotion.normalized * rollForce, combo = true });
        }

        if (Input.GetButtonDown("Shoot"))
        {
            TryCancelBackswing();
            Vector2 targetDirection = (LevelEditor.main.GetXYPlanePosition(Input.mousePosition) - (Vector2)transform.position).normalized;
            actions.Add(new Action() { type = State.ShootWindup, frames = shootWindupFrames, vector = -targetDirection * shootKnockback, sticky = true });
            actions.Add(new Action() { type = State.Shoot, frames = shootFrames });
        }

        if (Input.GetButtonDown("Attack"))
        {
            TryCancelBackswing();
            Vector2 targetDirection = (LevelEditor.main.GetXYPlanePosition(Input.mousePosition) - (Vector2)transform.position).normalized;
            actions.Add(new Action() { type = State.AttackWindup, frames = attackWindupFrames, vector = targetDirection * attackForce, sticky = true });
            actions.Add(new Action() { type = State.Attack, frames = attackFrames, vector = targetDirection, combo = true });
        }

        if (Input.GetButtonDown("Use item 1") && Items.Length >= 1)
        {
            Items[0].Activate(this);
        }

        if (Input.GetButtonDown("Use item 2") && Items.Length >= 2)
        {
            Items[1].Activate(this);
        }

        if (Input.GetButtonDown("Use item 3") && Items.Length >= 3)
        {
            Items[2].Activate(this);
        }
    }

    void UpdateLayer()
    {
        LayerMask targetLayer = shadow ? LayerMask.NameToLayer("Shadow") : LayerMask.NameToLayer("Player");
        gameObject.layer = targetLayer;
        foreach (Transform t in transform)
            t.gameObject.layer = targetLayer;
    }

    void OnGUI()
    {
        if (LevelEditor.main.mode >= EditMode.Create)
        {
            playerPanel.SetActive(false);
            return;
        }
        playerPanel.SetActive(true);
        keyIndicator.Amount = keys;
        ItemSlot[] itemSlots = playerPanel.GetComponentsInChildren<ItemSlot>();
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < keybinds.Length)
                itemSlots[i].slot.text = keybinds[i];
            if (i == 0)
                itemSlots[i].ItemSprite = swordIcon;
            else if (i == 1)
                itemSlots[i].ItemSprite = gunIcon;
            else if (i == 2)
                itemSlots[i].ItemSprite = dodgeIcon;
            else if (i - 3 < Items.Length)
            {
                itemSlots[i].ItemSprite = Items[i - 3].Icon;
            }
        }
    }
}
