using SgLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Ad : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    public void ClickBtn()
    {
        AdManager.ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {
                    CoinManager.Instance.AddCoins(50);
                    gameObject.SetActive(false);

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

    }


  private void Fail()
    {


        AdManager.ShowInterstitialAd("1lcaf5895d5l1293dc",
            () => {
                Debug.LogError("--插屏广告完成--");

            },
            (it, str) => {
                Debug.LogError("Error->" + str);
            });
    }



}
