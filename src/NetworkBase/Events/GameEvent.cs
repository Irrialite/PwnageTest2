using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkBase.Events
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameEvent : EventBase<EGameEventID>
    {
        public GameEvent(EGameEventID id, object data
#if !UNITY_5
            , object target
#endif
            ) : base(id, data
#if !UNITY_5
                , target
#endif
                )
        {
        }

        public static GameEvent Deserialize(string obj)
        {
            return JsonConvert.DeserializeObject<GameEvent>(obj);
        }
    }

    public enum EGameEventID
    {
        FrontPlayerRegister,        // 0
        FrontPlayerUnregister,
        GameStateSet,
        GameLoopStart,
        GameLoopEnd,
        FirstBetOnFieldSet,         // 5
        PlayerBets,
        PlayerDecision,
        DoneWithBetting,
        SelectedDenomination,
        InsuranceDecided,           // 10
        AllBetsCleared,
        LetItPlayDecided,
        BetSet,
        StartBettingTimer,
        BettingTimeSet,             // 15
        StopBettingTimer,
        CardEvent,
        InsuranceTimeSet,
        StopInsuranceTimer,
        PlayerCanExecuteDecision,   // 20
        PlayerDecisionEnded,
        ChangeBetFieldData,
        PlayerDecisionEnable,
        PlayerDecisionTimeSet,
        PlayerDecisionPause,        // 25
        PlayerDecisionResume,
        DecisionMessage,
        FinishGame,
        StartInsurance,
        BetEndResult,               // 30
        ClientConnected,
        NoFreePlayerSlot,
        BetSetHack,
        LoginAuthToken,
        MoneyState,                  // 35
        PlayerJoined,
        PlayerLeft,
        Handshake,
    }
}
