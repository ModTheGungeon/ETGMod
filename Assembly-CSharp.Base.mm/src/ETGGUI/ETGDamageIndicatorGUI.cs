using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ETGDamageIndicatorGUI : MonoBehaviour {

    private static List<DamageIndicator> Indicators = new List<DamageIndicator>();
    private static List<DamageIndicator> UsedPool = new List<DamageIndicator>();
    private static List<DamageIndicator> ToRemove = new List<DamageIndicator>();

    private static Dictionary<HealthHaver, HealthBar> AllBars = new Dictionary<HealthHaver, HealthBar>();
    public static Dictionary<HealthHaver, float> MaxHP = new Dictionary<HealthHaver, float>();
    public static Dictionary<HealthHaver, float> CurrentHP = new Dictionary<HealthHaver, float>();

    public static List<HealthHaver> ToRemoveBars = new List<HealthHaver>();

    public static bool RenderHealthBars = true;

    public static void Create() {
        GameObject newObject = new GameObject();
        newObject.name="Damage Indicators";
        newObject.AddComponent<ETGDamageIndicatorGUI>();
        newObject.transform.SetParent(ETGModGUI.MenuObject.transform);
    }

    public void Start() {
        DontDestroyOnLoad(gameObject);
    }

    public void Update() {
        foreach (DamageIndicator i in Indicators)
            i.Update();

        foreach (DamageIndicator i in ToRemove)
            Indicators.Remove(i);

        try {
            foreach (HealthBar bar in AllBars.Values)
                bar.Update();

            foreach (HealthHaver hh in ToRemoveBars) {
                AllBars.Remove(hh);
            }

            ToRemove.Clear();
            ToRemoveBars.Clear();
        }
        catch (System.Exception e) {
            Debug.Log(e.ToString());
        }
    }

    public void OnGUI() {
        foreach (DamageIndicator i in Indicators)
            i.OnGUI();

        foreach (HealthBar bar in AllBars.Values)
            bar.OnGUI();
    }

    public static void CreateIndicator(Vector3 worldPosOrigin, object content) {

        //We need to make a new object
        if (UsedPool.Count==0) {
            DamageIndicator newIndicator = new DamageIndicator();

            newIndicator.wPosOrigin=worldPosOrigin;
            newIndicator.content="<size=35>"+content+"</size>";

            Indicators.Add(newIndicator);
        } else {
            //We have a pooled object, use this instead

            DamageIndicator pickedIndicator = UsedPool[0];
            UsedPool.RemoveAt(0);

            pickedIndicator.content="<size=35>"+content+"</size>";
            pickedIndicator.wPosOrigin=worldPosOrigin;

            Indicators.Add(pickedIndicator);
        }
    }

    public static void CreateBar(HealthHaver targ) {

        if (AllBars.ContainsKey(targ))
            return;

        HealthBar newBar = new HealthBar();
        newBar.Target=targ;

        AllBars.Add(targ, newBar);
    }

    public static void UpdateHealthBar(HealthHaver hh, float dmg) {
        if (AllBars.ContainsKey(hh))
            AllBars[hh].UpdateTransitionBar(dmg);
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
                UsedPool.Add(this);
                ToRemove.Add(this);
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
        public HealthHaver Target;

        Rect OutlineRect = new Rect();
        Rect TotalBarRect = new Rect();
        Rect FilledBarRect = new Rect();
        Rect TransitionBarRect = new Rect();

        //The point on the HP bar we're transitioning to.
        float CurrentPoint = 1f;
        //The time before transition takes place.
        float TransitionDelay = 0.1f;
        float TransitionSpeed = 0.3f;

        public void Update() {

            if (Target==null) {
                ToRemoveBars.Add(Target);
                return;
            }

            if (TransitionDelay>0)
                TransitionDelay-=Time.deltaTime;
            else if (CurrentPoint>=CurrentHP[Target]/MaxHP[Target])
                CurrentPoint-=Time.deltaTime*TransitionSpeed;

        }

        public void UpdateTransitionBar(float damageDelta) {

            //Local hit point, the place we where at before we took damage
            float hitPointL = ( CurrentHP[Target]+damageDelta )/MaxHP[Target];

            //If the current hit point is greater than the local one, we'll leave it as-is. Otherwise we're going to set it to the current hit point.
            if (Mathf.Abs(CurrentPoint-hitPointL)<0.01f) {
                CurrentPoint=hitPointL;
            }

            TransitionDelay=0.1f;
        }

        public void OnGUI() {

            try {
                Vector3 wPos = (Vector3)Target.SpeculativeRigidbody_0.PixelCollider_0.UnitTopCenter;
                Vector2 screenPos = Camera.main.WorldToScreenPoint(wPos);
                screenPos=new Vector2(screenPos.x, Screen.height-screenPos.y);

                int hpMaxBar = (int)Mathf.Max(0, 15-( MaxHP[Target] ))+100;

                OutlineRect=new Rect(screenPos.x-( hpMaxBar/2 )-2, screenPos.y-12, hpMaxBar+4, 24);
                TotalBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar, 20);
                FilledBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar*( CurrentHP[Target]/MaxHP[Target] ), 20);
                TransitionBarRect=new Rect(screenPos.x-( hpMaxBar/2 ), screenPos.y-10, hpMaxBar*( CurrentPoint ), 20);

                GUI.color=new Color(0, 0, 0, 1);
                GUI.DrawTexture(OutlineRect, ETGModGUI.BoxTexture);
                GUI.color=new Color(151f/255f, 166f/255f, 170f/255f);
                GUI.DrawTexture(TotalBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.Lerp(new Color(229f/255f, 54f/255f, 23f/255f), new Color(77f/255f, 214f/255f, 80f/255f), CurrentHP[Target]/MaxHP[Target]);
                GUI.DrawTexture(FilledBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.Lerp(new Color(229f/255f, 54f/255f, 23f/255f), new Color(77f/255f, 214f/255f, 80f/255f), CurrentPoint)*0.5f;
                GUI.DrawTexture(TransitionBarRect, ETGModGUI.BoxTexture);
                GUI.color=Color.white;
            } catch {

            }
        }

    }

}

