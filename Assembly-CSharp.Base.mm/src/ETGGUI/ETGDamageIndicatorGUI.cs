using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ETGDamageIndicatorGUI : MonoBehaviour {

    private static List<DamageIndicator> indicators = new List<DamageIndicator>();
    private static List<DamageIndicator> usedPool = new List<DamageIndicator>();
    private static List<DamageIndicator> toRemove = new List<DamageIndicator>();

    private static Dictionary<HealthHaver, HealthBar> allBars = new Dictionary<HealthHaver, HealthBar>();
    public static Dictionary<HealthHaver, float> maxHP = new Dictionary<HealthHaver, float>();
    public static Dictionary<HealthHaver, float> currentHP = new Dictionary<HealthHaver, float>();

    public static List<HealthHaver> toRemoveBars = new List<HealthHaver>();

    public static bool RenderHealthBars = true;

    public static void Create() {
        GameObject newObject = new GameObject();
        newObject.name="Damage Indicators";
        newObject.AddComponent<ETGDamageIndicatorGUI>();
        newObject.transform.SetParent(ETGModGUI.menuObj.transform);
    }

    public void Start() {
        DontDestroyOnLoad(gameObject);
    }

    public void Update() {
        foreach (DamageIndicator i in indicators)
            i.Update();

        foreach (DamageIndicator i in toRemove)
            indicators.Remove(i);

        try {
            foreach (HealthBar bar in allBars.Values)
                bar.Update();

            foreach (HealthHaver hh in toRemoveBars) {
                allBars.Remove(hh);
            }

            toRemove.Clear();
            toRemoveBars.Clear();
        }
        catch (System.Exception e) {
            Debug.Log(e.ToString());
        }
    }

    public void OnGUI() {
        foreach (DamageIndicator i in indicators)
            i.OnGUI();

        foreach (HealthBar bar in allBars.Values)
            bar.OnGUI();
    }

    public static void CreateIndicator(Vector3 worldPosOrigin, object content) {

        //We need to make a new object
        if (usedPool.Count==0) {
            DamageIndicator newIndicator = new DamageIndicator();

            newIndicator.wPosOrigin=worldPosOrigin;
            newIndicator.content="<size=35>"+content+"</size>";

            indicators.Add(newIndicator);
        } else {
            //We have a pooled object, use this instead

            DamageIndicator pickedIndicator = usedPool[0];
            usedPool.RemoveAt(0);

            pickedIndicator.content="<size=35>"+content+"</size>";
            pickedIndicator.wPosOrigin=worldPosOrigin;

            indicators.Add(pickedIndicator);
        }
    }

    public static void CreateBar(HealthHaver targ) {

        if (allBars.ContainsKey(targ))
            return;

        HealthBar newBar = new HealthBar();
        newBar.target=targ;

        allBars.Add(targ, newBar);
    }

    public static void UpdateHealthBar(HealthHaver hh, float dmg) {
        if (allBars.ContainsKey(hh))
            allBars[hh].UpdateTransitionBar(dmg);
    }

    private class DamageIndicator {
        public Vector2 offset;
        public Vector3 wPosOrigin;
        public object content;

        private float time;

        public void Update() {
            offset-=Vector2.up*Time.deltaTime*5;
            offset=new Vector2(Mathf.Sin(time*15)-0.5f, offset.y);
            time+=Time.deltaTime;

            if (time>=2) {
                usedPool.Add(this);
                toRemove.Add(this);
                time=0;
                offset=Vector2.zero;
            }
        }

        public void OnGUI() {
            Vector2 worldToScreenPoint = Camera.main.WorldToScreenPoint(wPosOrigin);
            worldToScreenPoint=new Vector2(worldToScreenPoint.x, Screen.height-worldToScreenPoint.y);
            GUI.color=Color.red;
            GUI.Label(new Rect(worldToScreenPoint+( offset*15 )+( new Vector2(25, 25) ), new Vector2(50, 50)), content.ToStringIfNoString());
            GUI.color=Color.white;
        }
    }

    private class HealthBar {
        public HealthHaver target;

        Rect outlineRect = new Rect();
        Rect totalBarRect = new Rect();
        Rect filledBarRect = new Rect();
        Rect transitionBarRect = new Rect();

        //The point on the HP bar we're transitioning to.
        float currentPoint = 1f;
        //The time before transition takes place.
        float transitionDelay = 0.1f;
        float transitionSpeed = 0.3f;

        public void Update() {

            if (target==null) {
                toRemoveBars.Add(target);
                return;
            }

            if (transitionDelay>0)
                transitionDelay-=Time.deltaTime;
            else if (currentPoint>=currentHP[target]/maxHP[target])
                currentPoint-=Time.deltaTime*transitionSpeed;

        }

        public void UpdateTransitionBar(float damageDelta) {

            //Local hit point, the place we where at before we took damage
            float hitPointL = ( currentHP[target]+damageDelta )/maxHP[target];

            //If the current hit point is greater than the local one, we'll leave it as-is. Otherwise we're going to set it to the current hit point.
            if (Mathf.Abs(currentPoint-hitPointL)<0.01f) {
                currentPoint=hitPointL;
            }

            transitionDelay=0.1f;
        }

        public void OnGUI() {

            try {
                Vector3 wPos = (Vector3)target.SpeculativeRigidbody_0.PixelCollider_0.UnitTopCenter;
                Vector2 screenPos = Camera.main.WorldToScreenPoint(wPos);
                screenPos=new Vector2(screenPos.x, Screen.height-screenPos.y);

                int hpMaxBar = (int)Mathf.Max(0, 15-( maxHP[target] ))+100;

                outlineRect=new Rect(screenPos.x-( hpMaxBar/2 )-2, screenPos.y-12, hpMaxBar+4, 24);
                totalBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar, 20);
                filledBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar*( currentHP[target]/maxHP[target] ), 20);
                transitionBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar*( currentPoint ), 20);

                GUI.color=new Color(0, 0, 0, 1);
                GUI.DrawTexture(outlineRect, ETGModGUI.BoxTexture);
                GUI.color=new Color(151f/255f, 166f/255f, 170f/255f);
                GUI.DrawTexture(totalBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.Lerp(new Color(229f/255f, 54f/255f, 23f/255f), new Color(77f/255f, 214f/255f, 80f/255f), currentHP[target]/maxHP[target]);
                GUI.DrawTexture(filledBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.Lerp(new Color(229f/255f, 54f/255f, 23f/255f), new Color(77f/255f, 214f/255f, 80f/255f), currentPoint)*0.5f;
                GUI.DrawTexture(transitionBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.white;
            } catch {

            }
        }

    }

}

