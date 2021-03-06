﻿using UnityEngine;

public class Energy : MonoBehaviour, IDamageable, IRespawnable
{
    public int invulnFrames;

    private float current;
    public float Current { get { return current; } set { current = Mathf.Clamp(value, 0, limit); } }
    private float limit;
    public float Limit { get { return limit; } set { limit = Mathf.Clamp(value, 0, max); } }
    public float max;
    public float rechargeRate;
    public float energyToRechargeDelay; // How many recharge delay frames per point of energy consumed.
    public int minPenaltyFramesForZero; // How many recharge delay frames to add when hitting zero minimum (affects spamming at zero)
    public float damageToEnergy; // How much energy to take off per point of damage.
    public float damageToLimit; // How much energy limit to reduce per point of damage
    public float permanentDamageToEnergyGain; // How much temporary energy to regain once taking damage
    [EnumFlag]
    public DamageType vulnerableTo = DamageType.Generic | DamageType.Slash | DamageType.Explosive | DamageType.Ground;
    [ReadOnly]
    public float framesUntilRechargeBegins;
    [ReadOnly]
    public int framesSinceRechargeStarted;
    [ReadOnly]
    public int remInvulnFrames;

    [ReadOnly]
    public int framesSinceLastPulse;
    public float pulseThreshold;

    [ReadOnly]
    public EnergyIndicator indicator;

    [ReadOnly]
    public Checkpoint deathRespawnPoint;

    public ParticleSystem damageParticles;
    public ParticleSystem deathParticles;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public AudioClip pulseSound;
    public AudioClip dangerSound;

    ObjectData data;
    Rigidbody2D rb2d;

    void Start()
    {
        Current = Limit = max;
        data = GetComponentInParent<ObjectData>();
        rb2d = GetComponentInParent<Rigidbody2D>();
    }

    /// <summary>
    /// Uses up a certain amount of energy. Returns actual energy consumed.
    /// </summary>
    public float UseEnergy(float amt)
    {
        framesUntilRechargeBegins += amt * energyToRechargeDelay;
        float previous = Current;
        Current -= amt;
        if (current == 0)
        {
            AudioSource.PlayClipAtPoint(dangerSound, transform.position);
            framesUntilRechargeBegins = Mathf.Max(framesUntilRechargeBegins, minPenaltyFramesForZero);
        }
        return (previous - Current);
    }

    void FixedUpdate()
    {
        if (remInvulnFrames > 0)
            remInvulnFrames--;
        if (pulseSound)
        {
            framesSinceLastPulse++;
            if (current / max < pulseThreshold && framesSinceLastPulse >= Mathf.FloorToInt(current / max * 50 + 8))
            {
                framesSinceLastPulse = 0;
                AudioSource.PlayClipAtPoint(pulseSound, transform.position, 0.7f);
            }
        }

        if (framesUntilRechargeBegins > 0)
        {
            framesSinceRechargeStarted = 0;
            framesUntilRechargeBegins--;
        }
        else
            framesSinceRechargeStarted++;
        Current += rechargeRate * Mathf.Max(0, framesSinceRechargeStarted / 60f);
        if (indicator)
        {
            indicator.percentage = current / max;
            indicator.limitPercentage = limit / max;
        }
    }

    public bool Heal(int amt)
    {
        if (Limit == max)
            return false;
        Limit += amt * damageToLimit;
        Current += amt * damageToLimit;
        return true;
    }

    public void FullHeal()
    {
        Limit = max;
    }

    public int Damage(int dmg, GameObject source, Vector2 knockback, DamageType damageType = DamageType.Generic)
    {
        if (remInvulnFrames > 0 || (damageType | vulnerableTo) != vulnerableTo)
            dmg = 0;
        float permanentDamage = ConvertDamageToEnergy(dmg);
        framesSinceRechargeStarted = 0;
        if (knockback.magnitude > 0)
            rb2d.AddForce(knockback);
        if (permanentDamage > 0)
        {
            Current += permanentDamage * permanentDamageToEnergyGain;
            remInvulnFrames = invulnFrames;
            if (damageParticles && knockback.magnitude > 0 && (damageType | DamageType.Fall) != damageType)
                Instantiate(damageParticles, transform.position, Quaternion.LookRotation(knockback, Vector3.forward));
            if (hitSound)
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            CheckDeath();
        }
        return dmg;
    }

    void CheckDeath()
    {
        if (Limit > 0)
            return;
        if (deathParticles)
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        if (deathSound)
            Camera.main.GetComponent<AudioSource>().PlayOneShot(deathSound);
        if (deathRespawnPoint != null)
            Respawn();
        else
        {
            data.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Does damage to energy, converting it into permanent damage if necessary. Returns the actual permanent damage dealt to energy.
    /// </summary>
    float ConvertDamageToEnergy(int dmg)
    {
        float energy = dmg * damageToEnergy;
        float excess = Mathf.Max(0, energy - Current);
        float excessEnergyDamage = excess * damageToLimit / damageToEnergy;
        Current -= energy - excess;
        Limit -= excessEnergyDamage;
        return excessEnergyDamage;
    }

    public void SetDeathRespawnPoint(Checkpoint checkpoint)
    {
        if (deathRespawnPoint != null)
            deathRespawnPoint.active = false;
        deathRespawnPoint = checkpoint;
    }

    void Respawn()
    {
        data.transform.position = deathRespawnPoint.transform.position;
        FullHeal();
        rb2d.velocity = Vector2.zero;
    }
}
