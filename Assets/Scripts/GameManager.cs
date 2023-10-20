using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class UserPoints
{
    public int totalMoney;
    public List<int> randomNum;
    public List<int> skeletonRandomNum;
    public List<int> paylines;
    public int betBalance;
}

public class GameManager : MonoBehaviour {

    [Header("Text")]
    [SerializeField] TextMeshProUGUI _WinBalance;

    [Header("Button")]
    [SerializeField] Transform _payLinesParent;

    [Header("Components")]
    [SerializeField] Animator _animator;
    [SerializeField] Animator _errorAnim;

    [Header("Winning")]
    [SerializeField] List<GameObject> _winningIcons;
    [SerializeField] List<Transform> _winningSlots;
    [SerializeField] List<GameObject> _winningFrames;
    [SerializeField] GameObject _bigWinAnimation;

    List<int[,]> winningCombinations = new List<int[,]>();
    List<int> winningCombinationLines = new List<int>();
    public UserPoints user = new UserPoints
    {
        totalMoney = 0,
        randomNum = new List<int>(),
        skeletonRandomNum = new List<int>(),
        paylines = new List<int>(),
        betBalance = 0
    };

    private float _spinTime;

    private bool _guiActived {
                get { return _bigWinAnimation.activeSelf; }
                set { }
    }

    void Start()
    {
        _bigWinAnimation.SetActive(false);
    }

    public void StartSpin()
    {
        SendSignal();
    }

    private void SendSignal()
    {
        startSignal();
        SlotMachineReady();
    }

    private IEnumerator Spin()
    {
        _animator.SetTrigger("reels_start");

        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < _winningSlots.Count; i++)
        {
            //Destroy childs of winning slot 
            for (int j = 0; j < _winningSlots[i].childCount; j++)
            {
                _winningSlots[i].GetComponent<Image>().enabled = true;
                Destroy(_winningSlots[i].GetChild(0).gameObject);
            }
        }

        ToggleSlotsBlur(true);
        _spinTime = Random.Range(1f, 2.5f);
        yield return new WaitForSeconds(_spinTime - 0.3f);
        ToggleSlotsBlur(false);

        yield return new WaitForSeconds(0.45f);
        _animator.SetTrigger("reels_end");
        yield return new WaitForSeconds(0.15f);

        SetWinningCombination();

        yield return new WaitForSeconds(0.5f);
        EnableWiningFrames();
        yield return new WaitForSeconds(0.5f);

        if(winningCombinationLines.Count > 0)
        {
            _WinBalance.text = user.totalMoney.ToString("$0.00");
            _bigWinAnimation.SetActive(true);
            MenuManager.globalVariable._myBalance += (float)user.totalMoney;
        }

        GameObject.Find("MenuManager").GetComponent<MenuManager>().setEnableSpin();
        user = new UserPoints
        {
            totalMoney = 0,
            randomNum = new List<int>(),
            skeletonRandomNum = new List<int>(),
            paylines = new List<int>(),
            betBalance = 0
        };
    }

    private void SetWinningCombination()
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var idx = winningCombinations[0][j, i];

                _winningSlots[j + i * 3].GetComponent<Image>().enabled = false;

                var obj = Instantiate(_winningIcons[Math.Abs(idx) - 1], _winningSlots[j + i * 3]);
                obj.transform.localPosition = Vector3.zero;
                obj.GetComponent<Animation>().Stop();
                foreach (Transform child in obj.transform) {
                    var anim = child.GetComponent<Animation>();
                    if (anim) anim.Stop();
                }
            }
        }
    }

    private void EnableWiningFrames()
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var idx = winningCombinations[0][j, i];
                if (idx >= 0)
                {
                    var slot = _winningSlots[j + i * 3];
                    var anims = slot.GetComponentsInChildren<Animation>();
                    for (int k = 0; k < anims.Length; k++)
                        anims[k].Play();
                    _winningFrames[j + i * 3].SetActive(true);
                }
            }
        }

        for(int i = 0; i < winningCombinationLines.Count; i++)
        {
            var line = _payLinesParent.GetChild(winningCombinationLines[i] - 1);
            line.GetComponent<Animator>().SetTrigger("Highlighted");
        }
    }

    private void ToggleSlotsBlur(bool en)
    {
        foreach (Transform child in _animator.transform)
        {
            foreach (Transform subChild in child.transform)
            {
                var anim = subChild.GetComponent<Animator>();
                if (anim)
                {
                    if (en)
                        anim.SetTrigger("start_blur");
                    else
                        anim.SetTrigger("stop_blur");
                }
            }
        }
    }

    // Game Ready
    private void SlotMachineReady()
    {
        InitialPaylineAndWinframe();
        winningCombinationLines.Clear();
        winningCombinations.Clear();

        MenuManager.globalVariable._myBalance -= (float)MenuManager.globalVariable._totalBet;

        winningCombinations.Add(new int[,] { { user.skeletonRandomNum[0], user.skeletonRandomNum[1], user.skeletonRandomNum[2], user.skeletonRandomNum[3], user.skeletonRandomNum[4] },
                                             { user.skeletonRandomNum[5], user.skeletonRandomNum[6], user.skeletonRandomNum[7], user.skeletonRandomNum[8], user.skeletonRandomNum[9] },
                                             { user.skeletonRandomNum[10], user.skeletonRandomNum[11], user.skeletonRandomNum[12], user.skeletonRandomNum[13], user.skeletonRandomNum[14] }});

        for (int i = 0; i < user.paylines.Count; i++)
        {
            winningCombinationLines.Add(user.paylines[i]);
        }
        StartCoroutine(Spin());
    }

    // Format Paylines and WinFrames
    private void InitialPaylineAndWinframe()
    {
        for (int i = 0; i < winningCombinationLines.Count; i++)
        {
            // Set normal status
            var line = _payLinesParent.GetChild(winningCombinationLines[i] - 1);
            line.GetComponent<Animator>().SetTrigger("Normal");
        }

        for (int i = 0; i < _winningSlots.Count; i++)
        {
            // Disable winning frames
            for (int j = 0; j < _winningFrames.Count; j++)
                _winningFrames[j].SetActive(false);
        }
    }

    public void ToggleLines()
    {
        for (int i = 0; i < _payLinesParent.childCount; i++)
        {
            if (i < MenuManager.globalVariable._lines)
                _payLinesParent.GetChild(i).GetComponent<Button>().interactable = true;
            else
                _payLinesParent.GetChild(i).GetComponent<Button>().interactable = false;
        }
    }

    private void startSignal()
    {
        user.betBalance = MenuManager.globalVariable._bet;
        CreateRandomNumber();
        calcMatch();
    }

    private void CreateRandomNumber()
    {
        for(int i = 0; i < 15; i++){
            int randNum = Random.Range(1,11);
            user.randomNum.Add(randNum);
            user.skeletonRandomNum.Add(randNum * -1);
        }
    }

    private void calcMatch()
    {
        int lines = MenuManager.globalVariable._lines;
        int[][] numList = new int[][]{
            new int[]{9,8,7,6,5},
            new int[]{4,3,2,1,0},
            new int[]{14,13,12,11,10},
            new int[]{4,8,12,6,0},
            new int[]{14,8,2,6,10},
            new int[]{4,3,7,1,0},
            new int[]{14,13,7,11,10},
            new int[]{9,13,12,11,5},
            new int[]{9,3,2,1,5},
            new int[]{9,3,7,1,5},
            new int[]{9,13,7,11,5},
            new int[]{4,8,2,6,0},
            new int[]{14,8,12,6,10},
            new int[]{9,8,2,6,5},
            new int[]{9,8,12,6,5},
            new int[]{4,8,7,6,0},
            new int[]{14,8,2,6,10},
            new int[]{4,13,2,11,0},
            new int[]{4,13,12,11,0},
            new int[]{14,3,2,1,10},
            new int[]{14,3,12,1,10},
            new int[]{4,3,12,1,0},
            new int[]{14,13,2,11,10},
            new int[]{4,3,7,11,0},
            new int[]{4,3,7,1,10}
        };
        for (int i = 1; i <= lines; i++)
        {
            List<int> NumberStack = new List<int>();
            for (int j = 0; j < 5; j++)
            {
                NumberStack.Add(user.randomNum[numList[i-1][j]]);
            }
            int money = 0;
            money = countCheck(NumberStack);
            if (money > 0)
            {
                for (int j = 0; j < 5; j++)
                {
                    user.skeletonRandomNum[numList[i-1][j]] = user.randomNum[numList[i-1][j]];
                }
                user.paylines.Add(i);
            }
            user.totalMoney += money;
        }
    }

    private int countCheck(List<int> randomArray)
    {
        int setNum = 0;
        int count = 0;
        for (int i = 0; i < randomArray.Count; i++)
        {
            int count1 = 0;
            int temp = randomArray[i];
            for (int j = 0; j < randomArray.Count; j++)
            {
                if (temp == randomArray[j] || Math.Abs(temp) == 11)
                {
                    count1++;
                }
            }
            if (count1 >= 3)
            {
                setNum = randomArray[i];
                count = count1;
                break;
            }
        }
        return levelCheck(setNum, count);
    }

    private int levelCheck(int setNum, int count)
    {
        int[] levelPoint1 = new int[]{50, 40, 30, 20, 15, 15, 10, 10, 5, 5};
        int[] levelPoint2 = new int[]{400, 200, 150, 100, 75, 75, 50, 50, 25, 25};
        int[] levelPoint3 = new int[]{4000, 2000, 500, 400, 300, 300, 250, 250, 200, 200};
        if (count == 3)
        {
            return levelPoint1[setNum-1] * user.betBalance;
        } 
        else if(count == 4 && setNum < 3)
        {
            return levelPoint2[setNum-1] * user.betBalance;
        }
        else if(count == 400 && setNum > 3)
        {
            return levelPoint2[setNum-1] * user.betBalance;
        }
        else if(count == 5)
        {
            return levelPoint3[setNum-1] * user.betBalance;
        }
        else
            return 0;
    }
}
