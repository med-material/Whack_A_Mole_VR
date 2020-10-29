using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SelectionList : MonoBehaviour
{

    public class SelectionItem {
        public string label;
        public Toggle toggle;
    }

    public List<SelectionItem> selectionItems = new List<SelectionItem>();

    [Serializable]
    public class OnValueChanged : UnityEvent<int> {}
    public OnValueChanged onValueChanged; 

    private int value;

    public void Set(int val) {
        value = val;
        if (val < selectionItems.Count) {
            selectionItems[val].toggle.isOn = true;
        }
        onValueChanged.Invoke(value);
    }

    public int Get() {
        return value;
    }

    public string GetLabel(int value) {
        return selectionItems[value].label;
    }

    public GameObject itemTemplate;

    // Start is called before the first frame update
    void Awake()
    {
        itemTemplate.SetActive(false);
    }

    public void AddItems(List<string> newItems)
    {
        Transform templateTransform = itemTemplate.GetComponent<Transform>();
        for (int i = 0; i < newItems.Count; i++) {
            SelectionItem item = new SelectionItem();
            var obj = GameObject.Instantiate(itemTemplate);
            Toggle toggle = obj.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(x => OnSelectItem(item.toggle));
            Text text = obj.GetComponentInChildren<Text>();
            text.text = newItems[i];
            Transform tr = obj.GetComponent<Transform>();
            tr.SetParent(templateTransform.parent);
            obj.SetActive(true);
            item.label = newItems[i];
            item.toggle = toggle;
            selectionItems.Add(item);
        }

        if (value < selectionItems.Count) {
            selectionItems[value].toggle.isOn = true;
        }
    }

    public void SetInteractable(bool interactable) {
        foreach(var item in selectionItems) {
            item.toggle.interactable = interactable;
        }
    }

    public void ClearItems() {
        foreach(var item in selectionItems) {
            GameObject.Destroy(item.toggle.gameObject);
        }
        selectionItems.Clear();
    }

    private void OnSelectItem(Toggle toggle) {
        if (!toggle.isOn) {
            return;
        }
        int selectedIndex = -1;
        Transform tr = toggle.transform;
        Transform parent = tr.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i) == tr)
            {
                // Subtract one to account for template child.
                selectedIndex = i - 1;
                break;
            }
        }

        if (value == selectedIndex)
            return;

        if (selectedIndex < 0)
            return;
        
        value = selectedIndex;
        onValueChanged.Invoke(value);

    }
}
