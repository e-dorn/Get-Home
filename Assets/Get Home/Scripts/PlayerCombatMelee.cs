﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatMelee : MonoBehaviour
{
    public Animator animator;
    private Transform attackPoint;
    public LayerMask enemyLayers;
    float nextLightAttackTime = 0f;
    float nextHeavyAttackTime = 0f;
    float lastAttackTime = 0f;
    public GameObject display;
    WeaponManagement weaponManagement;
    //public GameObject equippedWeapon;
    public AnimationEvents events;
    //public Animator camAnim;

    public AudioSource attackSound;

    // weapon stats
    float lightAttackRange;
    float heavyAttackRange;
    int heavyAttackDamage;
    int lightAttackDamage;
    float lightAttackRate;
    float heavyAttackRate;
    int maxHits;

    // epipen
    public int epipenDamageBoost;
    public int epipenDuration;
    bool epipenActive;
    float startTimer; // timer to track when epipen was activated
    public Image epipenOverlay;
    public Image epipenTimer;

    // combos
    List<string> attacksList = new List<string>();
    Dictionary<List<string>, int> combos = new Dictionary<List<string>, int>(); // maps order of attacks for combo to the attack damage after combo
    bool comboActive;

    void Start()
    {
        events = gameObject.GetComponent<AnimationEvents>();
        weaponManagement = GetComponent<WeaponManagement>();
        epipenOverlay.enabled = false;
        epipenTimer.enabled = false;
        epipenTimer.transform.GetChild(0).GetComponent<Image>().enabled = false;
        AddCombos();
    }

    // Update is called once per frame
    void Update()
    {

        if (comboActive && Time.time - lastAttackTime >= 2 )
        {
            DisableCombo();
        }

        if (Time.time >= nextLightAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.E) && !events.isAttacking)
            {
                LightAttack();
                nextLightAttackTime = Time.time + 1f / lightAttackRate;
            }
        }

        if (Time.time >= nextHeavyAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.Q) && !events.isAttacking)
            {
                HeavyAttack();
                nextHeavyAttackTime = Time.time + 1f / heavyAttackRate;
            }
        }


        if (epipenActive && Time.time - startTimer > epipenDuration)
        {
            DisableEpipen();
        }
    }


    void LightAttack()
    {
        //camAnim.SetTrigger("shake");
        animator.SetTrigger("LightAttack");

        attackSound.Play();

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, lightAttackRange, enemyLayers);

        if (hitEnemies.Length == 0)
        {
            return;
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<EnemyHealth>().TakeDamage(lightAttackDamage);
            DealWCombo("LightAttack");
        }

        weaponManagement.AdjustDurability();

    }

    void HeavyAttack()
    {
        animator.SetTrigger("LightAttack");

        attackSound.Play();

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, heavyAttackRange, enemyLayers);

        if (hitEnemies.Length == 0)
        {
            return;
        }


        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<EnemyHealth>().TakeDamage(heavyAttackDamage);
            DealWCombo("HeavyAttack");
        }

        weaponManagement.AdjustDurability();
    }

    public void SetWeaponStats(GameObject weapon)
    {
        lightAttackRange = weapon.GetComponent<WeaponStats>().lightAttackRange;
        heavyAttackRange = weapon.GetComponent<WeaponStats>().heavyAttackRange;
        heavyAttackDamage = weapon.GetComponent<WeaponStats>().heavyAttackDamage;
        lightAttackDamage = weapon.GetComponent<WeaponStats>().lightAttackDamage;
        lightAttackRate = weapon.GetComponent<WeaponStats>().lightAttackRate;
        heavyAttackRate = weapon.GetComponent<WeaponStats>().heavyAttackRate;
        attackPoint = GameObject.Find(weapon.name + "AttackSpot").transform;

        if (epipenActive)
        {
            lightAttackDamage = lightAttackDamage * epipenDamageBoost;
            heavyAttackDamage = heavyAttackDamage * epipenDamageBoost;
        }
    }


    // Epipen Logic
    private IEnumerator EpipenTimer()
    {
        epipenOverlay.enabled = true;
        epipenTimer.enabled = true;
        epipenTimer.transform.GetChild(0).GetComponent<Image>().enabled = true;

        for (int i = epipenDuration; i > 0; i--)
        {
            epipenTimer.fillAmount = (float) i / epipenDuration;
            yield return new WaitForSeconds(1);
            epipenOverlay.enabled = !epipenOverlay.enabled;
        }

        DisableEpipen();

        //display.GetComponent<Text>().text = "";
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.name == "Epipen")
        {
            lightAttackDamage = lightAttackDamage * epipenDamageBoost;
            heavyAttackDamage = heavyAttackDamage * epipenDamageBoost;
            startTimer = Time.time;
            epipenActive = true;
            collider.gameObject.SetActive(false);
            StartCoroutine("EpipenTimer");
        }
    }

    public void DisableEpipen()
    {
        epipenActive = false;
        StopCoroutine("EpipenTimer");
        epipenTimer.fillAmount = 0;
        epipenOverlay.enabled = false;
        epipenTimer.enabled = false;
        epipenTimer.transform.GetChild(0).GetComponent<Image>().enabled = false;
        lightAttackDamage = lightAttackDamage / epipenDamageBoost;
        heavyAttackDamage = heavyAttackDamage / epipenDamageBoost;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        Gizmos.DrawWireSphere(attackPoint.position, lightAttackRange);
    }


    // Combo Logic
    void AddCombos()
    {
        combos.Add(new List<string>() {
            "LightAttack",
            "LightAttack",
            "HeavyAttack"}, 70);
    }

    void DealWCombo(string attack)
    {
        string S = "";

        if (comboActive)
        {
            DisableCombo();
            return;
        }

        if (lastAttackTime == 0 || Time.time - lastAttackTime <= 2)
        {
            attacksList.Add(attack);
            if (attacksList.Count > 3)
            {
                attacksList.RemoveAt(0);
            }
            foreach (string s in attacksList)
            {
                S += s;
            }
            Debug.Log(S);

            foreach (KeyValuePair<List<string>, int> c in combos)
            {
                if (c.Key.SequenceEqual(attacksList) && !epipenActive)
                {
                    display.GetComponent<Text>().text = "Combo Activated!";
                    lightAttackDamage = c.Value;
                    heavyAttackDamage = c.Value;
                    comboActive = true;
                    attacksList.Clear();
                }
            }

        }

        else
        {
            attacksList.Clear();
            attacksList.Add(attack);
        }

        lastAttackTime = Time.time;
    }

    public void DisableCombo()
    {
        display.GetComponent<Text>().text = "";
        comboActive = false;
        SetWeaponStats(weaponManagement.EquippedWeapon);
    }
}


