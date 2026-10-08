using System;
internal static class FrameMeterTests {
    static void Check(bool condition,string name){if(!condition)throw new Exception("Frame meter: "+name);}
    internal static void Run(){
        Check(!AdvantageCalculator.Calculate(new CharacterState(),new CharacterState()).HasValue,"unknown advantage");
        Check(AdvantageCalculator.Calculate(new CharacterState{ActionableFrame=100},new CharacterState{ActionableFrame=103})==3,"+3");
        Check(AdvantageCalculator.Calculate(new CharacterState{ActionableFrame=100},new CharacterState{ActionableFrame=99})==-1,"-1");
        Check(AdvantageCalculator.Calculate(new CharacterState{ActionableFrame=100},new CharacterState{ActionableFrame=100})==0,"even");
        using(var provider=new MockProvider()){
            int startup=0,active=0,recovery=0,hitstop=0;var history=new FrameHistory();bool multiActiveAfterRecovery=false,recovered=false;
            for(int i=0;i<420;i++){
                var f=provider.Poll();var p1=f.Meter.Players[0];var p2=f.Meter.Players[1];history.Add(f);
                Check(f.Meter.GameFrame==f.Number,"shared game clock");
                if(i<15){if(p1.Phase==FramePhase.Startup)startup++;if(p1.Phase==FramePhase.Active)active++;if(p1.Phase==FramePhase.Recovery)recovery++;if(p1.Phase==FramePhase.Hitstop){hitstop++;Check(p2.Phase==FramePhase.Hitstop,"shared hitstop");}}
                if(i==4||i==5||i==6)Check(AdvantageCalculator.Calculate(p1,p2)==3,"hitstop preserves advantage");
                if(i==64)Check(AdvantageCalculator.Calculate(p1,p2)==-1,"block disadvantage");
                if(i==184)Check(AdvantageCalculator.Calculate(p1,p2)==0,"trade equal");
                if(i==124)Check(!AdvantageCalculator.Calculate(p1,p2).HasValue,"whiff advantage unavailable");
                if(i>=240&&i<255){if(p1.Phase==FramePhase.Recovery)recovered=true;if(recovered&&p1.Phase==FramePhase.Active)multiActiveAfterRecovery=true;}
            }
            Check(startup==4&&active==3&&recovery==6&&hitstop==2,"exact 13 move frames plus 2 hitstop samples");
            Check(multiActiveAfterRecovery,"multi-hit active periods preserved");
            history.Select(5);var window=FrameTracker.Window(history,45);Check(window[window.Count-1]==history.Current,"meter / hitbox selection identical");Check(window.Count==6,"same interaction axis");
        }
        var native=GameStateReader.FromNative(new[]{45,1,0,1,1,8},new System.Collections.Generic.List<Box>{new Box{Group=1,Left=0,Bottom=0,Width=20,Height=20}});
        Check(native.Players[0].NativeStateId==45&&native.Players[0].Phase==FramePhase.ObservedAttack,"native attack region");
        Check(native.Players[1].Phase==FramePhase.Hitstop,"native hitstop flag");
        Check(!native.Players[0].StartupFrames.HasValue&&!native.Players[0].CurrentFrame.HasValue&&!AdvantageCalculator.Calculate(native.Players[0],native.Players[1]).HasValue,"native unknown timing preserved");
        var absent=GameStateReader.FromNative(new[]{-1,-1,0,-1,-1,0},new System.Collections.Generic.List<Box>());
        Check(absent.Players[0].Phase==FramePhase.Unknown&&!absent.Players[0].NativeStateId.HasValue,"absent fighter is unknown");
        var unknown=GameStateReader.Unavailable();Check(unknown.Players[0].Phase==FramePhase.Unknown&&!unknown.GameFrame.HasValue,"no fabricated live frame clock");
    }
}
