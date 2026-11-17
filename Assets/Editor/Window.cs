using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно отладки параметров автомобиля. Позволяет настраивать физику движения в реальном времени,
/// визуализировать данные и сохранять пресеты в ScriptableObject.
/// </summary>
public class Window : EditorWindow
{
    private Drive targetCar;
    private CarDebugConfig targetConfig;
    private SerializedObject serializedCar;
    private Vector2 scrollPos;

    // Свойства для поиска
    private SerializedProperty moveSpeedProp;
    private SerializedProperty accelerationProp;
    private SerializedProperty turnSpeedProp;
    private SerializedProperty normalGripProp;
    private SerializedProperty driftGripProp;

    // Данные для визуализации
    private float debugSpeed;
    private float debugLateralSpeed;
    private bool debugIsDrifting;

    /// <summary>
    /// Создает пункт меню для открытия окна отладки.
    /// </summary>
    [MenuItem("Tools/TimeRacers/Car Debugger")]
    public static void ShowWindow()
    {
        GetWindow<Window>("Car Debugger");
    }

    private void OnEnable()
    {
        // Пытаемся автоматически найти машину на сцене при открытии окна
        if (targetCar == null)
        {
            targetCar = FindObjectOfType<Drive>();
        }
        if (targetCar != null)
        {
            SetupSerializedObject();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Отладка автомобиля", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Выбор цели (машины)
        targetCar = (Drive)EditorGUILayout.ObjectField("Целевая машина", targetCar, typeof(Drive), true);

        if (targetCar == null)
        {
            EditorGUILayout.HelpBox("Выберите автомобиль на сцене или перетащите его в поле выше.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        if (serializedCar == null || serializedCar.targetObject != targetCar)
        {
            SetupSerializedObject();
        }

        serializedCar.Update();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // --- Блок настройки параметров ---
        EditorGUILayout.LabelField("Параметры движения", EditorStyles.boldLabel);
        if (moveSpeedProp != null && accelerationProp != null && turnSpeedProp != null && normalGripProp != null && driftGripProp != null)
        {
            EditorGUILayout.PropertyField(moveSpeedProp, new GUIContent("Макс. скорость"));
            EditorGUILayout.PropertyField(accelerationProp, new GUIContent("Ускорение"));
            EditorGUILayout.PropertyField(turnSpeedProp, new GUIContent("Скорость поворота"));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Сцепление (Grip)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(normalGripProp, new GUIContent("Обычное сцепление"));
            EditorGUILayout.PropertyField(driftGripProp, new GUIContent("Сцепление в дрифте"));
        }
        else
        {
            EditorGUILayout.HelpBox("Не удалось найти все необходимые поля в компоненте Drive. " +
                                    "Убедитесь, что имена полей совпадают.", MessageType.Error);
        }
        
        EditorGUILayout.Space();

        // --- Блок отладочной информации ---
        EditorGUILayout.LabelField("Текущее состояние (Play Mode)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Скорость: {debugSpeed:F2} ед/с");
        EditorGUILayout.LabelField($"Боковое скольжение: {debugLateralSpeed:F2} ед/с");
        EditorGUILayout.LabelField($"В дрифте: {(debugIsDrifting ? "Да" : "Нет")}");

        // Визуализация "здоровья" сцепления
        float gripHealth = Mathf.InverseLerp(0, 10f, Mathf.Abs(debugLateralSpeed));
        EditorGUILayout.LabelField("Сила скольжения:", $"{gripHealth:P0}");
        Rect rect = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.ProgressBar(rect, gripHealth, "Скольжение");

        EditorGUILayout.Space();

        // --- Блок кнопок быстрого действия ---
        EditorGUILayout.LabelField("Быстрые действия", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Сбросить настройки"))
        {
            ResetToDefaults();
        }
        if (GUILayout.Button("Применить ко всем машинам"))
        {
            ApplyToAllCars();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // --- Блок сохранения конфигурации ---
        EditorGUILayout.LabelField("Сохранение конфигурации", EditorStyles.boldLabel);
        targetConfig = (CarDebugConfig)EditorGUILayout.ObjectField(
            "Файл пресета", targetConfig, typeof(CarDebugConfig), false);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = targetConfig != null; // Кнопки активны только если выбран файл

        if (GUILayout.Button("Сохранить в пресет"))
        {
            SaveToConfig();
        }
        if (GUILayout.Button("Загрузить из пресета"))
        {
            LoadFromConfig();
        }

        GUI.enabled = true; // Возвращаем кнопки в строй
        EditorGUILayout.EndHorizontal();

        // Применяем изменения ползунков, если они были
        if (serializedCar.ApplyModifiedProperties())
        {
            SceneView.RepaintAll(); // Обновляем Gizmos в окне Scene
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Находит нужные SerializedProperty в компоненте Drive.
    /// </summary>
    private void SetupSerializedObject()
    {
        serializedCar = new SerializedObject(targetCar);
        moveSpeedProp = serializedCar.FindProperty("moveSpeed");
        accelerationProp = serializedCar.FindProperty("acceleration");
        turnSpeedProp = serializedCar.FindProperty("turnSpeed");
        normalGripProp = serializedCar.FindProperty("normalGrip");
        driftGripProp = serializedCar.FindProperty("driftGrip");
    }

    private void Update()
    {
        if (targetCar != null && Application.isPlaying)
        {
            debugSpeed = targetCar.CurrentSpeed;
            debugLateralSpeed = targetCar.CurrentLateralSpeed;
            debugIsDrifting = targetCar.IsDrifting;
            Repaint(); // Обновляем окно каждый кадр для отображения данных в реальном времени
        }
    }

    /// <summary>
    /// Сбрасывает параметры машины к значениям по умолчанию.
    /// </summary>
    private void ResetToDefaults()
    {
        if (targetCar == null) return;

        Undo.RecordObject(targetCar, "Reset Car Parameters");
        
        SerializedObject so = new SerializedObject(targetCar);
        so.FindProperty("moveSpeed").floatValue = 5f;
        so.FindProperty("acceleration").floatValue = 10f;
        so.FindProperty("turnSpeed").floatValue = 120f;
        so.FindProperty("normalGrip").floatValue = 12f;
        so.FindProperty("driftGrip").floatValue = 1.5f;
        so.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(targetCar);
        SceneView.RepaintAll();
    }

    /// <summary>
    /// Применяет текущие параметры выбранной машины ко всем машинам на сцене.
    /// </summary>
    private void ApplyToAllCars()
    {
        if (targetCar == null) return;

        Drive[] allCars = FindObjectsByType<Drive>(FindObjectsSortMode.None);
        
        SerializedObject sourceSo = new SerializedObject(targetCar);
        float mSpeed = sourceSo.FindProperty("moveSpeed").floatValue;
        float accel = sourceSo.FindProperty("acceleration").floatValue;
        float tSpeed = sourceSo.FindProperty("turnSpeed").floatValue;
        float nGrip = sourceSo.FindProperty("normalGrip").floatValue;
        float dGrip = sourceSo.FindProperty("driftGrip").floatValue;

        foreach (var car in allCars)
        {
            if (car == targetCar) continue;
            
            Undo.RecordObject(car, "Apply Car Parameters");
            
            SerializedObject carSo = new SerializedObject(car);
            carSo.FindProperty("moveSpeed").floatValue = mSpeed;
            carSo.FindProperty("acceleration").floatValue = accel;
            carSo.FindProperty("turnSpeed").floatValue = tSpeed;
            carSo.FindProperty("normalGrip").floatValue = nGrip;
            carSo.FindProperty("driftGrip").floatValue = dGrip;
            carSo.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(car);
        }
        Debug.Log($"Параметры применены к {allCars.Length - 1} машинам.");
    }

    /// <summary>
    /// Сохраняет текущие параметры выбранной машины в ScriptableObject.
    /// </summary>
    private void SaveToConfig()
    {
        if (targetCar == null || targetConfig == null) return;

        SerializedObject carSo = new SerializedObject(targetCar);
        
        targetConfig.moveSpeed = carSo.FindProperty("moveSpeed").floatValue;
        targetConfig.acceleration = carSo.FindProperty("acceleration").floatValue;
        targetConfig.turnSpeed = carSo.FindProperty("turnSpeed").floatValue;
        targetConfig.normalGrip = carSo.FindProperty("normalGrip").floatValue;
        targetConfig.driftGrip = carSo.FindProperty("driftGrip").floatValue;

        EditorUtility.SetDirty(targetConfig);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Параметры машины сохранены в пресет: {targetConfig.name}");
    }

    /// <summary>
    /// Загружает параметры из ScriptableObject и применяет их к машине.
    /// </summary>
    private void LoadFromConfig()
    {
        if (targetCar == null || targetConfig == null) return;

        Undo.RecordObject(targetCar, "Load Car Config");

        SerializedObject carSo = new SerializedObject(targetCar);
        
        carSo.FindProperty("moveSpeed").floatValue = targetConfig.moveSpeed;
        carSo.FindProperty("acceleration").floatValue = targetConfig.acceleration;
        carSo.FindProperty("turnSpeed").floatValue = targetConfig.turnSpeed;
        carSo.FindProperty("normalGrip").floatValue = targetConfig.normalGrip;
        carSo.FindProperty("driftGrip").floatValue = targetConfig.driftGrip;
        
        carSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(targetCar);
        
        Debug.Log($"Параметры загружены из пресета: {targetConfig.name}");
    }
}