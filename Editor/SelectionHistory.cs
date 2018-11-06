using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace FknMetal.Editor.SelectionHistory
{
    [InitializeOnLoad]
	public class SelectionHistory
	{
        private static readonly string statePrefsKey = "fknmetal.selectionhistory.state";
        private const int historyDepth = 50;
        private static readonly object _lock = new object();
        private static SelectionHistory instance;
        private State state;
        private bool changingSelection;
        
        [System.Serializable]
        public class HistoryEntry
        {
            public int[] instanceIds;
            public int activeContextId;
        }

        [System.Serializable]
        public class State
        {
            public long processStartTicks;
            public List<HistoryEntry> undo = new List<HistoryEntry>();
            public List<HistoryEntry> redo = new List<HistoryEntry>();
        }

        private void Serialize()
        {
            EditorPrefs.SetString(statePrefsKey, JsonUtility.ToJson(state));
        }

        private void Deserialize()
        {
            var processStart = System.Diagnostics.Process.GetCurrentProcess().StartTime.Ticks;

            var data = EditorPrefs.GetString(statePrefsKey);
            if (!string.IsNullOrEmpty(data)) {
                try {
                    state = JsonUtility.FromJson<State>(data);

                    // Not supporting saving/restoring history when editor opens/closes since
                    // instance IDs are not persistable.
                    if (processStart != state.processStartTicks) {
                        Debug.LogFormat("Clearing selection history, from different process");
                        state = null;
                    }
                }
                catch (System.Exception e) {
                    Debug.LogException(e);
                }
            }

            if (state == null) {
                state = new State();
                state.processStartTicks = processStart;
            }
        }

        public void Clear()
        {
            state = new State();
            state.processStartTicks = System.Diagnostics.Process.GetCurrentProcess().StartTime.Ticks;
            EditorPrefs.DeleteKey(statePrefsKey);
        }

        public void Undo()
        {
            Assert.IsFalse(changingSelection);

            try {
                changingSelection = true;

                // Top of undo is current selection, so if we don't have another entry there is nothing to do.
                if (state.undo.Count <= 1) {
                    return;
                }

                while (state.undo.Count > 0 && !SelectionChanged()) {
                    state.redo.Add(state.undo[state.undo.Count-1]);
                    state.undo.RemoveAt(state.undo.Count-1);
                }

                Object[] objects = Selection.objects;

                while (state.undo.Count > 0) {
                    var current = state.undo[state.undo.Count-1];
                    var _objects = current.instanceIds.Select(i => EditorUtility.InstanceIDToObject(i)).Where(o => o != null).ToArray();

                    // Special case, objects don't exist anymore and originally did (e.g. not a null selection).
                    // Skip this entry, delete it, and move to next oldest.
                    if (_objects.Length == 0 && current.instanceIds.Length > 0) {
                        state.undo.RemoveAt(state.undo.Count-1);
                        continue;
                    }

                    objects = _objects;
                    break;
                } 

                Selection.objects = objects;
            }
            finally {
                changingSelection = false;
            }
        }

        public void Redo()
        {
            Assert.IsFalse(changingSelection);

            try {
                changingSelection = true;

                if (state.redo.Count == 0) {
                    return;
                }

                Object[] objects = Selection.objects;

                while (state.redo.Count > 0) {
                    var current = state.redo[state.redo.Count-1];
                    state.redo.RemoveAt(state.redo.Count-1);

                    var _objects = current.instanceIds.Select(i => EditorUtility.InstanceIDToObject(i)).Where(o => o != null).ToArray();

                    // Special case, objects don't exist anymore and originally did (e.g. not a null selection).
                    // Skip this entry, delete it, and move to next oldest.
                    if (_objects.Length == 0 && current.instanceIds.Length > 0) {
                        continue;
                    }

                    state.undo.Add(current);
                    objects = _objects;
                    break;
                } 

                Selection.objects = objects;
            }
            finally {
                changingSelection = false;
            }
        }

        private void UpdateHistory()
        {
            if (changingSelection) {
                return;
            }

            if (state.undo.Count > 0 && !SelectionChanged()) {
                return;
            }

            state.redo.Clear();

            var entry = new HistoryEntry{
                activeContextId = Selection.activeContext != null ? Selection.activeContext.GetInstanceID() : -1,
                instanceIds = Selection.objects.Select(o => o.GetInstanceID()).ToArray(),
            };

            state.undo.Add(entry);

            while (state.undo.Count > historyDepth) {
                state.undo.RemoveAt(0);
            }
        }

        private bool SelectionChanged()
        {
            var current = state.undo[state.undo.Count-1];
            
            if (current.instanceIds.Length != Selection.objects.Length) {
                return true;
            }

            for (int i = 0; i < current.instanceIds.Length; i++) {
                if (current.instanceIds[i] != Selection.objects[i].GetInstanceID()) {
                    return true;
                }
            }

            return false;
        }

        private SelectionHistory()
        {
            Deserialize();
        }

        [MenuItem("Tools/Selection History/Previous %#z")]
        public static void UndoHistory()
        {
            Instance.Undo();
        }

        [MenuItem("Tools/Selection History/Next %#y")]
        public static void NextHistory()
        {
            Instance.Redo();
        }

        [MenuItem("Tools/Selection History/Clear")]
        public static void ClearHistory()
        {
            Instance.Clear();
        }

        private static SelectionHistory Instance
        {
            get {
                lock (_lock) {
                    if (instance == null) {
                        instance = new SelectionHistory();
                    }

                    return instance;
                }
            }
        }

        static SelectionHistory()
        {
            EditorApplication.quitting += () => {
                lock (_lock) {
                    if (instance != null) {
                        instance.Clear();
                    }
                }
            };

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnBeforeAssemblyReload()
        {
            Instance.Serialize();
        }

        private static void OnAfterAssemblyReload()
        {
            // Force creation of instance
            var _ = Instance;
        }

        private static void OnSelectionChanged()
        {
            Instance.UpdateHistory();
        }
    }
}
