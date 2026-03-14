using UnityEngine;

namespace Manager
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }
        
        [Header("References")]
        public TMPro.TextMeshProUGUI countTmp;

        // 
        private static int _coins = 0;
        public static int Coins => _coins;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void AddCoins(int amount)
        {
            _coins += amount;
            countTmp.text = _coins.ToString();
            Debug.Log($"[CurrencyManager] AddCoins {amount}, total:{_coins}");
        }
    }
}