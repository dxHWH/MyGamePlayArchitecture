using UnityEngine;
using GamePlayArchitecture;

// 【1. 直接在这个文件定义枚举，精简文件数量】
namespace GamePlayArchitecture
{
    // 阵营定义
    public enum EFaction
    {
        Neutral, // 中立
        Player,  // 玩家阵营
        RedAI,   // 红色帝国
        BlueAI   // 蓝色联盟
    }

    // 【新增】：阵营协议接口。只要实现了这个接口的灵魂，就拥有了阵营概念
    public interface IFactionMember
    {
        EFaction FactionId { get; set; }
    }
}

// 【2. 统筹全服分数的专属计分板】
public class SurvivalGameState : AGameState
{
    public int PlayerScore { get; private set; }
    public int RedAIScore { get; private set; }
    public int BlueAIScore { get; private set; }

    // 开放给外界（Controller 或 Pawn）调用的加分接口
    public void AddScore(EFaction faction, int amount)
    {
        switch (faction)
        {
            case EFaction.Player:
                PlayerScore += amount;
                Log.N($"<color=cyan>[GameState] 玩家得分！当前分数: {PlayerScore}</color>");
                break;
            case EFaction.RedAI:
                RedAIScore += amount;
                Log.N($"<color=red>[GameState] 红色帝国得分！当前分数: {RedAIScore}</color>");
                break;
            case EFaction.BlueAI:
                BlueAIScore += amount;
                Log.N($"<color=blue>[GameState] 蓝色联盟得分！当前分数: {BlueAIScore}</color>");
                break;
        }

        // 未来如果你做了 UI，可以在这里通过 EventSystem.Instance.Trigger 发送分数更新广播
    }
}