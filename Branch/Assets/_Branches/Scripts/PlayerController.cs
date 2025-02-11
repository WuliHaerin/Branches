using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using SgLib;

public enum GameEvent
{
    Start,
    Paused,
    GameOver
}

public class PlayerController : MonoBehaviour
{
    public static event System.Action PlayerDie;

    [Header("Object references")]
    public ParentPlayerController parentPlayerController;
    public GameManager gameManager;
    public UIManager uiManager;
    public AnimationClip dieAnim;
    public ParticleSystem coinParticle;
    public float timeToDestroyParticle = 0.5f;
    //How long particle exists

    [HideInInspector]
    public bool isPlayerRunning = false;
    //Check the player started to run
    [HideInInspector]
    public bool touchDisable = true;
    //Disable touch
    [HideInInspector]
    public bool isRotatingTrunk = false;
    [HideInInspector]
    public bool isBlockedByBranch = false;
    //Check the player hit the branch
       
    private ParticleSystem particleTemp;
    private Vector3 dirRotate;
    private bool check = true;
    private bool isPlayerRotated = false;
    private float rotateAngle;
    private float fixedAngle = 0f;
    private int checkRotateTrunk = 0;
    private int dirTurn;
    private int dieType = 0;

    public GameObject AdPanel;
    
    public void SetAdPanel(bool a)
    {
        AdPanel.SetActive(a);
        Time.timeScale = a ? 0 : 1;
    }

    public void PreDie()
    {
        SetAdPanel(true);
    }

    public void DieOne()
    {
        GetComponent<Animator>().StopPlayback();

        // Fire event
        if (PlayerDie != null)
        {
            PlayerDie();
        }
    }

    public void DieTwo()
    {

        SoundManager.Instance.PlaySound(SoundManager.Instance.hitTrunk);

        if (dirRotate == Vector3.right)
        {
            Vector3 endPos = new Vector3(parentPlayerController.transform.position.x, parentPlayerController.transform.position.y, 0.7f);
            StartCoroutine(MoveParentPlayer(0.3f, endPos));
        }
        else
        {
            Vector3 endPos = new Vector3(parentPlayerController.transform.position.x, parentPlayerController.transform.position.y, -0.7f);
            StartCoroutine(MoveParentPlayer(0.3f, endPos));
        }

        touchDisable = true;
        isPlayerRunning = false;
        GetComponent<Animator>().Play(dieAnim.name);
        StartCoroutine(EnableDestroyTrunk());

        // Fire event
        if (PlayerDie != null)
        {
            PlayerDie();
        }
    }

    public bool isCancelAd = false;
    public void CancelAd()
    {
        SetAdPanel(false);
        dieType = 0;
        if(dieType==1)
        {
            DieOne();
        }
        else if(dieType==2)
        {
            DieTwo();
        }
        dieType = 0;
        isCancelAd = true;
    }

    public void Revive()
    {
        AdManager.ShowVideoAd("192if3b93qo6991ed0",
           (bol) => {
               if (bol)
               {
                   StartCoroutine(RotateTrunk(dirRotate));                  
                   Vector3 endPos = new Vector3(parentPlayerController.transform.position.x + 2.5f, parentPlayerController.transform.position.y, parentPlayerController.transform.position.z); ;
                   parentPlayerController.gameObject.transform.position = endPos;
                   dieType = 0;
                   //StartCoroutine(Invincible());

                   AdManager.clickid = "";
                   AdManager.getClickid();
                   AdManager.apiSend("game_addiction", AdManager.clickid);
                   AdManager.apiSend("lt_roi", AdManager.clickid);


               }
               else
               {
                   StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
               }
           },
           (it, str) => {
               Debug.LogError("Error->" + str);
               //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
           });
        SetAdPanel(false);



    }

    //public bool isInvincible;

    //public IEnumerator Invincible()
    //{
    //    isInvincible = true;
    //    yield return new WaitForSeconds(3);
    //    isInvincible = false;
    //}

    void OnEnable()
    {
        GameManager.GameStateChanged += GameManager_GameStateChanged;
    }

    void OnDisable()
    {
        GameManager.GameStateChanged -= GameManager_GameStateChanged;
    }

    // Use this for initialization
    void Start()
    {
        // Switch to the selected character
        GameObject currentCharacter = CharacterManager.Instance.characters[CharacterManager.Instance.CurrentCharacterIndex];
        Mesh charMesh = currentCharacter.GetComponent<MeshFilter>().sharedMesh;
        Material charMaterial = currentCharacter.GetComponent<Renderer>().sharedMaterial;
        GetComponent<MeshFilter>().mesh = charMesh;
        GetComponent<MeshRenderer>().material = charMaterial;    

        // Turn the character to face toward user
        transform.rotation = Quaternion.Euler(0, 180, 0);

        // Initial setup
        ScoreManager.Instance.Reset();
        dirTurn = 1;
        dirRotate = Vector3.right;

        // Start counting score coroutine
        StartCoroutine(CountScore());
    }

    void GameManager_GameStateChanged(GameState newState, GameState oldState)
    {
        if (newState == GameState.Playing && oldState == GameState.Prepare)
        {
            touchDisable = false;
            isPlayerRunning = true;

            // Let the character turn and face toward his move direction
            if (!isPlayerRotated)
            {
                isPlayerRotated = true;
                StartCoroutine(RotatePlayer());
            } 
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the player lags behind the camera view --> game over
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (isPlayerRunning && screenPos.x < -70 && gameManager.GameState != GameState.GameOver)
        {
            if(!isCancelAd)
            {
                dieType = 1;
                PreDie();
            }
            else
            {
                DieOne();
            }
        }

        if (gameManager.GameState == GameState.Playing)
        {              
            if (!isRotatingTrunk)
            {
                //Draw ray ahead of player and check if hit the branch, stop moving player
                Ray rayRight = new Ray(transform.position + new Vector3(0, 0.5f, 0), Vector3.right);
                RaycastHit hit;
                if (Physics.Raycast(rayRight, out hit, 0.5f))
                {
                    if (hit.collider.tag == "Branch")
                    {
                        isBlockedByBranch = true;
                    }
                }
                else
                {
                    isBlockedByBranch = false;
                }
            }
                    
            if (Input.GetMouseButtonDown(0) && !touchDisable)
            { 
                // Turn the trunk
                checkRotateTrunk++;
                if (checkRotateTrunk > 0)
                {
                    if (dirRotate == Vector3.right)
                    {
                        rotateAngle = 90f;
                    }
                    else
                    {
                        rotateAngle = -90f;
                    }
                    StartCoroutine(RotateTrunk(dirRotate));
                }
            }
        }

        if ((ScoreManager.Instance.Score != 0) && (ScoreManager.Instance.Score % gameManager.changeRotateDirectionScore == 0) && check)
        {
            check = false;
            dirTurn = dirTurn * (-1);
            if (dirTurn < 0)
            {
                dirRotate = Vector3.left;
            }
            else
            {
                dirRotate = Vector3.right;
            }
            StartCoroutine(WaitAndEnableCheck());
        }
    }

    IEnumerator CountScore()
    {
        float countInterval = 1;
        float timePast = 0;

        while (true)
        {
            if (gameManager.GameState == GameState.GameOver)
            {
                yield break;
            }
            else if (gameManager.GameState == GameState.Playing && isPlayerRunning)
            {
                timePast += Time.deltaTime;
                if (timePast >= countInterval)
                {
                    ScoreManager.Instance.AddScore(1);
                    timePast = 0;
                }
            }

            yield return null;
        }
    }

    //Rotate trunk with a direction
    IEnumerator RotateTrunk(Vector3 dir)
    {
        if (isRotatingTrunk)
            yield break;

        isRotatingTrunk = true;

        SoundManager.Instance.PlaySound(SoundManager.Instance.rotateTrunk);

        float currentAngle = 0f;

        if (dir == Vector3.right)
        {
            fixedAngle += rotateAngle;
            while (currentAngle < rotateAngle)
            {
                float rotateAmount = gameManager.trunkRotatingSpeed * Time.deltaTime;
                rotateAmount = currentAngle + rotateAmount > rotateAngle ? rotateAngle - currentAngle : rotateAmount;
                gameManager.transform.Rotate(dir * rotateAmount);
                currentAngle += rotateAmount;
                yield return null;
            }
            gameManager.transform.eulerAngles = new Vector3(fixedAngle, 0, 0);
        }
        else
        {
            fixedAngle += rotateAngle;
            while (currentAngle > rotateAngle)
            {
                float rotateAmount = gameManager.trunkRotatingSpeed * Time.deltaTime;
                rotateAmount = currentAngle - rotateAmount < rotateAngle ? currentAngle - rotateAngle : rotateAmount;
                gameManager.transform.Rotate(dir * rotateAmount);
                currentAngle -= rotateAmount;
                yield return null;
            }
            gameManager.transform.eulerAngles = new Vector3(fixedAngle, 0, 0);
        }

        isRotatingTrunk = false;
    }

    void OnTriggerEnter(Collider other)
    {
        //if player hit the branch by rotation, player die
        if (other.tag == "Branch" && !isBlockedByBranch)
        {
            if(!isCancelAd)
            {
                dieType = 2;
                PreDie();
            }
            else
            {
                DieTwo();
            }
        }

        //Player hit the gold
        if (other.tag == "Gold")
        {
            SoundManager.Instance.PlaySound(SoundManager.Instance.coin);
            CoinManager.Instance.AddCoins(1);
            particleTemp = (ParticleSystem)Instantiate(coinParticle, other.gameObject.transform.position, coinParticle.transform.rotation);
            particleTemp.Simulate(0.5f, true, false);
            particleTemp.Play();
            Destroy(particleTemp, timeToDestroyParticle);
            Destroy(other.gameObject);
        }
    }

    IEnumerator EnableDestroyTrunk()
    {
        yield return new WaitForSeconds(dieAnim.length * 5);
        isPlayerRunning = true;
        GetComponent<MeshRenderer>().enabled = false;
    }

    IEnumerator MoveParentPlayer(float timeMove, Vector3 endPos)
    {
        float t = 0;
        while (t < timeMove)
        {
            float fraction = t / timeMove;
            Vector3 startPos = parentPlayerController.gameObject.transform.position;
            parentPlayerController.gameObject.transform.position = Vector3.Lerp(startPos, endPos, fraction);
            t += Time.deltaTime;
            yield return null;
        }
        parentPlayerController.transform.position = endPos;
    }


    IEnumerator RotatePlayer()
    {
        float currentAngle = 0;
        while (currentAngle < 90)
        {
            float rotateAmount = 200f * Time.deltaTime;
            transform.Rotate(Vector3.down * rotateAmount);
            currentAngle += rotateAmount;
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, 90, 0);
    }

    IEnumerator WaitAndEnableCheck()
    {
        yield return new WaitForSeconds(3f);
        check = true;
    }
}