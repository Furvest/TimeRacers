using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно отладки параметров автомобиля. Позволяет настраивать ScriptableObject (CarStats) в реальном времени,
/// визуализировать данные и управлять пресетами конфигураций.
/// </summary>
public class Window : EditorWindow
{
    private Drive targetCar;
    private CarStats activeStats;
    private CarStats presetStats;
    
    private SerializedObject serializedStats;
    private Vector2 scrollPos;

    // Данные для визуализации
    private float debugSpeed;
    private float debugLateralSpeed;
    private bool debugIsDrifting;

    [MenuItem("Tools/TimeRacers/Car Debugger")]
    public static void ShowWindow()
    {
        GetWindow<Window>("Car Debugger");
    }

    private void OnEnable()
    {
        if (targetCar == null)
        {
            targetCar = FindObjectOfType<Drive>();
        }
        UpdateTargetStats();
    }

    private void UpdateTargetStats()
    {
        if (targetCar != null && targetCar.Stats != null)
        {
            activeStats = targetCar.Stats;
            if (serializedStats == null || serializedStats.targetObject != activeStats)
            {
                serializedStats = new SerializedObject(activeStats);
            }
        }
        else
        {
            activeStats = null;
            serializedStats = null;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Отладка автомобиля", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Выбор машины
        targetCar = (Drive)EditorGUILayout.ObjectField("Целевая машина", targetCar, typeof(Drive), true);

        if (targetCar == null)
        {
            EditorGUILayout.HelpBox("Выберите автомобиль на сцене.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        // Обновляем ссылку на конфиг, если машина сменилась
        UpdateTargetStats();

        if (activeStats == null)
        {
            EditorGUILayout.HelpBox("У выбранной машины не назначен CarStats! Перетащите ассет в компонент Drive.", MessageType.Error);
            EditorGUILayout.EndVertical();
            return;
        }

        serializedStats.Update();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // --- Блок настройки параметров ---
        EditorGUILayout.LabelField($"Активный конфиг: {activeStats.name}", EditorStyles.boldLabel);
        
        SerializedProperty iterator = serializedStats.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.name == "m_Script") continue;
            EditorGUILayout.PropertyField(iterator, true);
        }
        
        EditorGUILayout.Space();

        // --- Блок отладочной информации ---
        EditorGUILayout.LabelField("Текущее состояние (Play Mode)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Скорость: {debugSpeed:F2} ед/с");
        EditorGUILayout.LabelField($"Боковое скольжение: {debugLateralSpeed:F2} ед/с");
        EditorGUILayout.LabelField($"В дрифте: {(debugIsDrifting ? "Да" : "Нет")}");

        float gripHealth = Mathf.InverseLerp(0, 10f, Mathf.Abs(debugLateralSpeed));
        Rect rect = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.ProgressBar(rect, gripHealth, "Скольжение");

        EditorGUILayout.Space();

        // --- Блок сохранения конфигурации (Пресеты) ---
        EditorGUILayout.LabelField("Менеджер пресетов", EditorStyles.boldLabel);
        
        presetStats = (CarStats)EditorGUILayout.ObjectField(
            "Файл-пресет", presetStats, typeof(CarStats), false);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = presetStats != null;

        if (GUILayout.Button("Сохранить в пресет"))
        {
            SaveToPreset();
        }
        if (GUILayout.Button("Загрузить из пресета"))
        {
            LoadFromPreset();
        }
        
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // Применяем изменения ползунков к ScriptableObject
        if (serializedStats.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(activeStats); // Помечаем ассет как измененный (чтобы сохранилось на диск)
            SceneView.RepaintAll(); // Обновляем Gizmos
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void Update()
    {
        if (targetCar != null && Application.isPlaying)
        {
            debugSpeed = targetCar.CurrentSpeed;
            debugLateralSpeed = targetCar.CurrentLateralSpeed;
            debugIsDrifting = targetCar.IsDrifting;
            Repaint();
        }
    }

    /// <summary>
    /// Копирует все значения из активного конфига машины в выбранный файл-пресет.
    /// </summary>
    private void SaveToPreset()
    {
        if (activeStats == null || presetStats == null) return;

        Undo.RecordObject(presetStats, "Save to Preset");
        
        // Копируем все значения
        presetStats.moveSpeed = activeStats.moveSpeed;
        presetStats.reverseSpeed = activeStats.reverseSpeed;
        presetStats.acceleration = activeStats.acceleration;
        presetStats.reverseAcceleration = activeStats.reverseAcceleration;
        presetStats.brakeDeceleration = activeStats.brakeDeceleration;
        presetStats.drag = activeStats.drag;
        presetStats.turnSpeed = activeStats.turnSpeed;
        presetStats.driftTurnBoost = activeStats.driftTurnBoost;
        presetStats.minTurnSpeed = activeStats.minTurnSpeed;
        presetStats.rwdOversteer = activeStats.rwdOversteer;
        presetStats.normalGrip = activeStats.normalGrip;
        presetStats.driftGrip = activeStats.driftGrip;
        presetStats.driftDragMultiplier = activeStats.driftDragMultiplier;
        presetStats.boostDuration = activeStats.boostDuration;
        presetStats.boostSpeedMultiplier = activeStats.boostSpeedMultiplier;
        presetStats.boostAccelerationMultiplier = activeStats.boostAccelerationMultiplier;
        presetStats.boostImpulse = activeStats.boostImpulse;

        EditorUtility.SetDirty(presetStats);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Пресет '{presetStats.name}' успешно обновлен!");
    }

    /// <summary>
    /// Копирует все значения из файла-пресета в активный конфиг машины.
    /// </summary>
    private void LoadFromPreset()
    {
        if (activeStats == null || presetStats == null) return;

        Undo.RecordObject(activeStats, "Load from Preset");
        
        // Копируем все значения
        activeStats.moveSpeed = presetStats.moveSpeed;
        activeStats.reverseSpeed = presetStats.reverseSpeed;
        activeStats.acceleration = presetStats.acceleration;
        activeStats.reverseAcceleration = presetStats.reverseAcceleration;
        activeStats.brakeDeceleration = presetStats.brakeDeceleration;
        activeStats.drag = presetStats.drag;
        activeStats.turnSpeed = presetStats.turnSpeed;
        activeStats.driftTurnBoost = presetStats.driftTurnBoost;
        activeStats.minTurnSpeed = presetStats.minTurnSpeed;
        activeStats.rwdOversteer = presetStats.rwdOversteer;
        activeStats.normalGrip = presetStats.normalGrip;
        activeStats.driftGrip = presetStats.driftGrip;
        activeStats.driftDragMultiplier = presetStats.driftDragMultiplier;
        activeStats.boostDuration = presetStats.boostDuration;
        activeStats.boostSpeedMultiplier = presetStats.boostSpeedMultiplier;
        activeStats.boostAccelerationMultiplier = presetStats.boostAccelerationMultiplier;
        activeStats.boostImpulse = presetStats.boostImpulse;

        EditorUtility.SetDirty(activeStats);
        AssetDatabase.SaveAssets();
        
        // Обновляем отображение в окне
        if (serializedStats != null)
        {
            serializedStats.Update();
        }
        
        Debug.Log($"Пресет '{presetStats.name}' применен к машине!");
    }
}