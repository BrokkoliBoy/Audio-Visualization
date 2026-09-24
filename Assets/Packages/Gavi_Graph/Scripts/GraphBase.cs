using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Utility;

public class GraphBase : MonoBehaviour
{
    // base class for the two graph classes

    [Header("----- Settings -----")]
    [SerializeField] protected bool m_showGraph;
    [SerializeField] protected int m_maxNumberPoints = 50; // how many points in x-dimension, sets the precision

    // if a lot of values are put into the graph, the values can be batched. One point on the graph will represent multiple values. The values within a batch are averaged
    [Header("--- Batching ---")]
    [SerializeField] private int m_initialBatchSize = 1;

    [Header("--- Chart Size ---")]
    [SerializeField] protected float m_minHighestValue; // if > 0, this value will be the lower y value shown on the graph
    [SerializeField] protected float m_maxHighestValue; // if > 0, this value will be the upper y value shown on the graph. If == 0, the upper bound will always be the highest value of the graph line
    [SerializeField] protected bool m_allowOvershot; // if this is set and m_maxHighestValue is > 0, values > m_maxHighestValue will overshoot and be higher than m_maxHighestValue. Otherwise, the graphline will set those values to m_maxHighestValue

    [Header("--- Transform ---")]
    [SerializeField] protected Vector3 m_relativePosition;
    [SerializeField] protected float m_width;
    [SerializeField] protected float m_height;

    [Header("--- Stipes ---")]
    [SerializeField] protected int m_stripeNumberHorizontal = 4;

    [Header("--- Rescaling ---")]
    [SerializeField] protected float m_rescaleYTime = 0.3f;
    [SerializeField] protected float m_rescaleYLerpFactor = 0.97f;
    [SerializeField] protected float m_initialMaxChartValueY = 1f;

    [Header("--- Texts ---")]
    [SerializeField] protected bool m_showHorizontalTexts;
    [SerializeField] protected Vector3 m_textCurrentRelativePosition;
    [SerializeField] protected Vector3 m_textFirstRelativePosition;
    [SerializeField] protected Vector3 m_textsStripesVerticalRelativePosition;
    [SerializeField] protected Vector3 m_textsStripesHorizontalRelativePosition = new Vector3(-0.5f, 0, 0);
    [SerializeField] protected Vector3 m_textStripeCurrentNumberRelativePosition;


    [Header("--- Prefabs ---")]
    [SerializeField] protected GameObject m_lineRendererValuesPrefab;
    [SerializeField] protected GameObject m_lineRendererStripesPrefab;
    [SerializeField] protected GameObject m_lineRendererGraphPrefab;
    //[SerializeField] protected GameObject m_pointPrefab;
    [SerializeField] protected GameObject m_emptyPrefab;
    [SerializeField] protected GameObject m_textCurrentPrefab;
    [SerializeField] protected GameObject m_textMinPrefab;
    [SerializeField] protected GameObject m_textFirstPrefab;
    [SerializeField] protected GameObject m_textStripesHorizontalPrefab;
    [SerializeField] protected GameObject m_textStripesVerticalPrefab;


    // positions
    private Vector3 m_centerPosition;
    protected float m_maxChartValueY;

    // value lists
    protected List<float> m_batchedValues = new List<float>();
    protected List<float> m_currentBatchValues = new List<float>();

    // lines
    protected LineRenderer m_valueLine;
    protected LineRenderer m_axisHorizontal;
    protected LineRenderer m_axisVertical;
    protected LineRenderer m_stripeCurrentNumber;
    protected List<LineRenderer> m_stripesHorizontal;
    protected List<LineRenderer> m_stripesVertical;

    // parent transforms
    protected Transform m_graphLinesTransform;
    protected Transform m_valueLinesTransform;
    protected Transform m_stripeLinesTransform;
    protected Transform m_textsTransform;

    // Texts
    protected TextMesh m_textCurrentValue;
    protected TextMesh m_textFirst;
    protected List<TextMesh> m_textsStripesVertical;
    protected List<TextMesh> m_textsStripesHorizontal;
    protected TextMesh m_textCurrentNumber;

    // rescaling
    protected float m_timePassedRescaleY;
    protected float m_maxChartValueYCurrent;
    protected float m_oldMaxChartValueY;
    protected bool m_isRescalingY;

    // misc
    protected int m_numberRawValues;
    private bool m_wasShowHorizontalTexts;
    protected int m_batchSize = 1;

    #region Mono
    protected void OnStart()
    {
        if (m_showGraph)
        {
            CreateBaseGraph();
        }
    }
    private void Update()
    {
        ManageRescaleY();
        if ((m_wasShowHorizontalTexts && !m_showHorizontalTexts) || (!m_wasShowHorizontalTexts && m_showHorizontalTexts))
            UpdateStripesHorizontal();
        m_wasShowHorizontalTexts = m_showHorizontalTexts;
    }
    #endregion

    #region Init
    private void CreateBaseGraph()
    {
        // this method mainly creates parent transforms for transparency, initializes lists and positions the lines and texts of the graph

        m_centerPosition = transform.position + m_relativePosition;

        // create parent objects
        GameObject valueLinesParent = Instantiate(m_emptyPrefab, transform);
        m_valueLinesTransform = valueLinesParent.transform;
        m_valueLinesTransform.position = m_centerPosition;
        m_valueLinesTransform.gameObject.name = "Value Line Parent";
        GameObject graphLinesParent = Instantiate(m_emptyPrefab, transform);
        m_graphLinesTransform = graphLinesParent.transform;
        m_graphLinesTransform.position = m_centerPosition;
        m_graphLinesTransform.gameObject.name = "Graph Lines Parent";
        GameObject stripeLinesParent = Instantiate(m_emptyPrefab, transform);
        m_stripeLinesTransform = stripeLinesParent.transform;
        m_stripeLinesTransform.position = m_centerPosition;
        m_stripeLinesTransform.gameObject.name = "Stripe Lines Parent";

        // create graph lines
        GameObject horizontal = Instantiate(m_lineRendererGraphPrefab, m_graphLinesTransform);
        m_axisHorizontal = horizontal.GetComponent<LineRenderer>();
        GameObject vertical = Instantiate(m_lineRendererGraphPrefab, m_graphLinesTransform);
        m_axisVertical = vertical.GetComponent<LineRenderer>();

        // create value line
        GameObject valueLine = Instantiate(m_lineRendererValuesPrefab, m_valueLinesTransform);
        m_valueLine = valueLine.GetComponent<LineRenderer>();
        m_valueLine.positionCount = 0;

        // create Lists
        //m_rawValues = new List<float>();
        m_batchedValues = new List<float>();
        m_currentBatchValues = new List<float>();
        m_stripesVertical = new List<LineRenderer>();
        m_stripesHorizontal = new List<LineRenderer>();
        m_textsStripesVertical = new List<TextMesh>();
        m_textsStripesHorizontal = new List<TextMesh>();

        // create stripe lines
        // vertical
        CreateVerticalStripes();
        // horizontal
        int numberStripesHorizontal = m_stripeNumberHorizontal;
        for (int i = 0; i < numberStripesHorizontal; i++)
        {
            GameObject stripesHorizontalGameObject = Instantiate(m_lineRendererStripesPrefab, m_stripeLinesTransform);
            stripesHorizontalGameObject.name = "Stripe Horizontal";
            LineRenderer stripe = stripesHorizontalGameObject.GetComponent<LineRenderer>();
            m_stripesHorizontal.Add(stripe);
        }

        // create stripe for current number of training units absolved
        GameObject stripeCurrentGameObject = Instantiate(m_lineRendererStripesPrefab, m_stripeLinesTransform);
        LineRenderer stripeCurrent = stripeCurrentGameObject.GetComponent<LineRenderer>();
        m_stripeCurrentNumber = stripeCurrent;

        // create Texts
        GameObject textTransform = Instantiate(m_emptyPrefab, transform);
        m_textsTransform = textTransform.transform;
        m_textsTransform.position = m_centerPosition;
        m_textsTransform.name = "Texts Parent";
        if (m_textCurrentPrefab != null)
        {
            GameObject textCurrent = Instantiate(m_textCurrentPrefab, m_textsTransform);
            textCurrent.name = "Text Current";
            m_textCurrentValue = textCurrent.GetComponent<TextMesh>();
        }
        if (m_textFirstPrefab != null)
        {
            GameObject textFirst = Instantiate(m_textFirstPrefab, m_textsTransform);
            textFirst.name = "Text Max";
            m_textFirst = textFirst.GetComponent<TextMesh>();
        }
        // stripe Texts
        if (m_textStripesVerticalPrefab != null)
        {
            for (int i = 0; i < m_textsStripesVertical.Count; i++)
            {
                GameObject textStripe = Instantiate(m_textStripesVerticalPrefab, m_textsTransform);
                textStripe.name = "Text Stripe Vertical " + i;
                TextMesh text = textStripe.GetComponent<TextMesh>();
                m_textsStripesVertical.Add(text);
            }
        }
        if (m_textStripesHorizontalPrefab != null)
        {
            for (int i = 0; i < m_stripeNumberHorizontal; i++)
            {
                GameObject textStripe = Instantiate(m_textStripesHorizontalPrefab, m_textsTransform);
                textStripe.name = "Text Stripe Horizontal " + i;
                TextMesh text = textStripe.GetComponent<TextMesh>();
                m_textsStripesHorizontal.Add(text);
            }
        }

        // text current cost
        GameObject textStripeCurrent = Instantiate(m_textStripesVerticalPrefab, m_textsTransform);
        textStripeCurrent.name = "Text Stripe Current";
        m_textCurrentNumber = textStripeCurrent.GetComponent<TextMesh>();

        m_batchSize = m_initialBatchSize;
        m_maxChartValueYCurrent = m_initialMaxChartValueY;
        PositionGraphLines();
        UpdateStripesVertical();
        UpdateStripesHorizontal();
    }
    private void PositionGraphLines()
    {
        // called once at Start
        m_axisHorizontal.SetPosition(0, new Vector3(0, 0, 0));
        m_axisHorizontal.SetPosition(1, new Vector3(m_width, 0, 0));

        m_axisVertical.SetPosition(0, new Vector3(0, 0, 0));
        m_axisVertical.SetPosition(1, new Vector3(0, m_height, 0));
    }
    protected virtual void CreateVerticalStripes()
    {
        // implemented in the child class
    }
    #endregion

    #region Update Positions Stripes/Texts
    protected void UpdateTextCurrent()
    {
        if (m_textCurrentValue == null)
            return;

        m_textCurrentValue.transform.position = m_valueLinesTransform.position + new Vector3(m_width, m_valueLine.GetPosition(m_valueLine.positionCount - 1).y, 0) + m_textCurrentRelativePosition;
        float value = m_batchedValues[m_batchedValues.Count - 1];
        string text = "";
        if (value < 0.00001f)
            text = value.ToString("#.#######");
        else if (value < 0.0001f)
            text = value.ToString("#.######");
        else if (value < 0.001f)
            text = value.ToString("#.#####");
        else if (value < 0.01f)
            text = value.ToString("#.####");
        else if (value < 0.1f)
            text = value.ToString("#.###");
        else
            text = value.ToString("#.##");

        m_textCurrentValue.text = text;
    }
    protected void UpdateTextHighesValue()
    {
        if (m_textFirst == null)
            return;
        m_textFirst.transform.position = m_valueLinesTransform.position + new Vector3(0, m_height, 0) + m_textFirstRelativePosition;
        float value = m_maxChartValueY;
        string text = "";
        if (value < 0.00001f)
            text = value.ToString("#.#######");
        else if (value < 0.0001f)
            text = value.ToString("#.######");
        else if (value < 0.001f)
            text = value.ToString("#.#####");
        else if (value < 0.01f)
            text = value.ToString("#.####");
        else if (value < 0.1f)
            text = value.ToString("#.###");
        else
            text = value.ToString("#.##");

        m_textFirst.text = text;
    }
    protected virtual void UpdateStripesVertical()
    {
        // implemented in the child class
    }
    protected void UpdateStripesHorizontal()
    {
        if (m_maxChartValueYCurrent == 0 || m_stripeNumberHorizontal == 0)
            return;

        float max = m_maxChartValueYCurrent;

        float upperBound = -1;
        float lowerBound = -1;
        if (max > 1)
        {
            lowerBound = 1f;
            for (int i = 0; i < 100; i++)
            {
                if (lowerBound < max)
                {
                    lowerBound *= 2f;
                    continue;
                }
                else
                    break;
            }
            upperBound = lowerBound * Mathf.Pow(2, m_stripeNumberHorizontal);
        }
        else if (max < 1)
        {
            upperBound = 1f;
            for (int i = 0; i < 100; i++)
            {
                if (upperBound > max)
                {
                    upperBound *= 0.5f;
                    continue;
                }
                else
                    break;
            }
            lowerBound = upperBound * Mathf.Pow(0.5f, m_stripeNumberHorizontal - 1);
        }
        else { lowerBound = 0; upperBound = 1; }


        float currentValue = lowerBound;
        for (int i = 0; i < m_stripesHorizontal.Count; i++)
        {
            float valueY = ValueToPositionY(currentValue);
            m_stripesHorizontal[i].SetPosition(0, new Vector3(0, valueY, 0));
            m_stripesHorizontal[i].SetPosition(1, new Vector3(m_width, valueY, 0));
            if (m_showHorizontalTexts)
            {
                m_textsStripesHorizontal[i].transform.position = transform.position + m_relativePosition + new Vector3(0, valueY, 0) + m_textsStripesHorizontalRelativePosition;
                m_textsStripesHorizontal[i].text = currentValue + "";
            }
            else
            {
                m_textsStripesHorizontal[i].text = "";
            }
            currentValue *= 2f;
        }
    }
    protected void UpdateCurrentNumber() // current number = number of currently absolved training units
    {
        if (m_stripeCurrentNumber != null)
        {
            float positionX = m_width;

            Vector3 position = m_textsTransform.position + new Vector3(positionX, 0, 0) + m_textStripeCurrentNumberRelativePosition;
            m_textCurrentNumber.transform.position = position;
            m_textCurrentNumber.text = "" + m_numberRawValues;
        }
    }
    #endregion

    #region Rescaling / Shifting
    protected void ChangeMaxValueY(float value)
    {
        if (m_minHighestValue > 0)
            value = Mathf.Max(m_minHighestValue, value);
        if (m_maxHighestValue > 0)
            value = Mathf.Min(m_maxHighestValue, value);

        m_isRescalingY = true;
        m_oldMaxChartValueY = m_maxChartValueYCurrent;
        m_maxChartValueY = value;
        m_timePassedRescaleY = 0;
    }
    private void ManageRescaleY()
    {
        if (!m_isRescalingY || m_maxChartValueY == 0)
            return;

        // Rescale via time
        if (m_rescaleYTime > 0)
        {
            m_timePassedRescaleY += Time.deltaTime;
            if (m_timePassedRescaleY < m_rescaleYTime)
            {
                float f = m_timePassedRescaleY / m_rescaleYTime;
                m_maxChartValueYCurrent = Mathf.Lerp(m_oldMaxChartValueY, m_maxChartValueY, f);
            }
            else
            {
                m_maxChartValueYCurrent = m_maxChartValueY;
                m_isRescalingY = false;
            }
        }

        // Rescale via lerp
        if (m_rescaleYLerpFactor > 0 && m_rescaleYLerpFactor < 1)
        {
            if (m_maxChartValueYCurrent / m_maxChartValueY < 0.99f || m_maxChartValueYCurrent / m_maxChartValueY > 1.01f)
            {
                m_maxChartValueYCurrent = Mathf.Lerp(m_maxChartValueY, m_maxChartValueYCurrent, m_rescaleYLerpFactor);
            }
            else
            {
                m_maxChartValueYCurrent = m_maxChartValueY;
                m_isRescalingY = false;
            }
        }

        RescaleY();
    }
    private void RescaleY()
    {
        for (int i = 0; i < m_valueLine.positionCount; i++)
        {
            m_valueLine.SetPosition(i, new Vector3(m_valueLine.GetPosition(i).x, ValueToPositionY(m_batchedValues[i]), 0));
        }
        UpdateStripesHorizontal();
    }
    #endregion

    #region Value Control
    public virtual void AddValue(float value)
    {
        m_numberRawValues++;
        if (!m_showGraph)
            return;

        UpdateCurrentNumber();

        m_currentBatchValues.Add(value);
        if (m_currentBatchValues.Count >= m_batchSize)
        {
            float batchedValue = 0;
            // batched value is average of raw values
            foreach (float v in m_currentBatchValues)
                batchedValue += v;
            batchedValue /= m_currentBatchValues.Count;

            AddBatchedValue(batchedValue);
            ReInitializeBatch();
        }
    }
    protected virtual void AddBatchedValue(float value)
    {
        // implemented in child class
    }
    public virtual void AddValues(List<float> values)
    {
        foreach (float value in values)
            AddValue(value);
    }
    public virtual void AddValues(float[] values)
    {
        foreach (float value in values)
            AddValue(value);
    }
    #endregion

    #region Misc
    private void ReInitializeBatch()
    {
        m_currentBatchValues.Clear();
    }
    protected void AddPositionToLine(Vector3 worldPosition)
    {
        m_valueLine.positionCount += 1;
        m_valueLine.SetPosition(m_valueLine.positionCount - 1, worldPosition);
    }
    protected float ValueToPositionY(float value)
    {
        float min = 0;
        float max = m_maxChartValueYCurrent;
        if (!m_allowOvershot)
            value = Mathf.Clamp(value, min, max);
        return Utility.MapValuePercent(min, max, value) * m_height;
    }
    protected float GetHighestY(ref List<float> values)
    {
        float highest = 0;
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] > highest)
                highest = values[i];
        }
        return highest;
    }
    public void PauseShow()
    {
        gameObject.SetActive(false);
    }
    public void UnpauseShow()
    {
        gameObject.SetActive(true);
    }
    #endregion
}
