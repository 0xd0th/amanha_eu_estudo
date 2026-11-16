using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChoiceSceneBuilder
{
    const string ScenePath="Assets/_game/scenes/choice_prototype.unity";
    [MenuItem("Tools/Game/Create Choice Prototype")]
    public static void EnsureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || System.IO.File.Exists(ScenePath)) return;
        Build();
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent,false); r.sizeDelta=size; r.anchoredPosition=pos; return r;
    }
    static Image Image(string name,Transform parent,Vector2 size,Vector2 pos,Color color)
    {
        var r=Rect(name,parent,size,pos); var i=r.gameObject.AddComponent<Image>(); i.color=color;i.raycastTarget=false;return i;
    }
    static Text Text(string name,Transform parent,string value,int fontSize,Vector2 size,Vector2 pos)
    {
        var t=Rect(name,parent,size,pos).gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/_game/art/fonts/pressstart2p.ttf");t.text=value;t.fontSize=fontSize;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
    }
    static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero; }
    static ChoiceImpact Impact(string id,string label,float amount) => new ChoiceImpact {statId=id,label=label,amount=amount};
    static void Build()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try {
            SceneManager.SetActiveScene(scene);
            var root=new GameObject("ChoiceSystem");root.AddComponent<ChoiceSceneInput>();
            var controller=root.AddComponent<ChoiceController>();
            var canvas=new GameObject("ChoiceCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(root.transform);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var panel=Image("PhoneChoicePanel",canvas.transform,Vector2.zero,Vector2.zero,new Color(0,0,0,.65f));Stretch(panel.rectTransform);panel.raycastTarget=true;
            var view=panel.gameObject.AddComponent<PhoneChoiceView>();
            var phone=Rect("PhoneRoot",panel.transform,new Vector2(600,1000),new Vector2(0,-20));
            Image("HandPlaceholder",phone,new Vector2(520,650),new Vector2(65,-240),new Color(.75f,.43f,.28f));
            Image("PhoneFramePlaceholder",phone,new Vector2(500,940),Vector2.zero,new Color(.06f,.07f,.09f));
            var screen=Image("ScreenContent",phone,new Vector2(450,870),Vector2.zero,new Color(.13f,.16f,.23f));
            view.contextText=Text("ContextText",screen.transform,"",24,new Vector2(410,40),new Vector2(0,380));
            view.promptText=Text("PromptText",screen.transform,"",29,new Vector2(410,160),new Vector2(0,270));
            view.resultText=Text("ResultText",screen.transform,"",20,new Vector2(410,75),new Vector2(0,-385));
            var viewport=Rect("OptionsViewport",screen.transform,new Vector2(410,510),new Vector2(0,-80));viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=viewport;scroll.movementType=ScrollRect.MovementType.Clamped;
            var content=Rect("Content",viewport,new Vector2(410,0),Vector2.zero);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=14;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;view.content=content;
            var card=Image("ChoiceCard",content,new Vector2(410,150),Vector2.zero,new Color(.24f,.28f,.36f));card.raycastTarget=true;
            var cv=card.gameObject.AddComponent<ChoiceCardView>();cv.button=card.gameObject.AddComponent<Button>();cv.button.targetGraphic=card;
            card.gameObject.AddComponent<LayoutElement>().preferredHeight=150;
            cv.titleText=Text("Title",card.transform,"",26,new Vector2(380,40),new Vector2(0,48));
            cv.descriptionText=Text("Description",card.transform,"",19,new Vector2(380,30),new Vector2(0,12));
            cv.reasonText=Text("UnavailableReason",card.transform,"",17,new Vector2(380,25),new Vector2(0,-59));
            var row=Rect("AffectedStats",card.transform,new Vector2(380,36),new Vector2(0,-25));var hl=row.gameObject.AddComponent<HorizontalLayoutGroup>();hl.childAlignment=TextAnchor.MiddleCenter;hl.spacing=12;hl.childForceExpandWidth=false;hl.childForceExpandHeight=false;cv.impactRow=row;
            var badge=Rect("StatBadge",row,new Vector2(110,32),Vector2.zero);var le=badge.gameObject.AddComponent<LayoutElement>();le.preferredWidth=110;le.preferredHeight=32;
            Image("Icon",badge,new Vector2(18,18),new Vector2(-42,0),Color.white);
            Text("StatName",badge,"",17,new Vector2(90,32),new Vector2(12,0));cv.impactTemplate=badge.gameObject;badge.gameObject.SetActive(false);
            System.IO.Directory.CreateDirectory("Assets/_game/prefabs/ui");
            var prefab=PrefabUtility.SaveAsPrefabAsset(card.gameObject,"Assets/_game/prefabs/ui/choice_card.prefab");view.cardTemplate=prefab.GetComponent<ChoiceCardView>();Object.DestroyImmediate(card.gameObject);
            var data=ScriptableObject.CreateInstance<ChoiceDefinition>();data.id="exam_invitation";data.context="faculdade";data.prompt="a prova é amanhã e seus amigos chamaram para sair. o que você faz?";
            data.options.Add(new ChoiceOption{id="study",title="estudar agora",description="revisar o conteúdo da prova",impacts=new System.Collections.Generic.List<ChoiceImpact>{Impact("intelligence","inteligência",10),Impact("health","saúde",-5)}});
            data.options.Add(new ChoiceOption{id="friends",title="encontrar a galera",description="aceitar o convite",impacts=new System.Collections.Generic.List<ChoiceImpact>{Impact("social","social",10),Impact("health","saúde",-3)}});
            data.options.Add(new ChoiceOption{id="rest",title="descansar",description="recuperar as energias",impacts=new System.Collections.Generic.List<ChoiceImpact>{Impact("health","saúde",8)}});
            data.options.Add(new ChoiceOption{id="gym",title="ir à academia",available=false,unavailableReason="academia fechada",impacts=new System.Collections.Generic.List<ChoiceImpact>{Impact("health","saúde",5)}});
            System.IO.Directory.CreateDirectory("Assets/_game/data/choices");AssetDatabase.CreateAsset(data,"Assets/_game/data/choices/exam_invitation.asset");
            var so=new SerializedObject(controller);so.FindProperty("initialChoice").objectReferenceValue=data;so.FindProperty("view").objectReferenceValue=view;so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Debug.Log("Choice prototype created: " + ScenePath);
        } finally {SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
    }
}
