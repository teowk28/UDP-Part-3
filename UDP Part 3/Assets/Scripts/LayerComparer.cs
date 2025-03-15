using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using static UnityEditor.Timeline.TimelinePlaybackControls;
using System.Linq;


public class LayerComparer: IComparer<VisualElement>
{
    public int Compare(VisualElement lhs, VisualElement rhs) 
    {
        if(lhs.style.top.value.value > rhs.style.top.value.value)
            return 1;
        if (lhs.style.top.value.value == rhs.style.top.value.value)
            return 0;
        else
            return -1;
    }
}

public class IntLayerComparer: IComparer<int>
{
    public List<VisualElement> reference;
    public int Compare(int lhs, int rhs) 
    {
        if(reference[lhs].style.top.value.value > reference[rhs].style.top.value.value)
            return 1;
        if (reference[lhs].style.top.value.value == reference[rhs].style.top.value.value)
            return 0;
        else
            return -1;
    }
}
