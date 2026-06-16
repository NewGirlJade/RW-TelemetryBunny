using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using Newtonsoft.Json;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace TelemetryLogger;

[BepInPlugin("NewGirlJade.TelemetryLogger", "TelemetryLogger", "0.0.1")]

public class TelemetryLogger : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "NewGirlJade.TelemetryLogger";
    public const string PLUGIN_NAME = "TelemetryLogger";
    private StreamWriter telemetryfile;
    private bool special_pressed = false;
    private bool should_log = false;
    
    void Awake()
    {
        string logPath = Path.Combine(Paths.GameRootPath, "telemetry.log.jsonl");
        telemetryfile = new StreamWriter(logPath, append:true) { AutoFlush = true};
        Logger.LogInfo($"Logging to: {logPath}");
        telemetryfile.WriteLine(DateTime.Now.ToString(CultureInfo.CurrentCulture));
    }

    void OnDestroy()
    { 
        telemetryfile?.Close();
        telemetryfile?.Dispose();
}

    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }

    private bool IsInit;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (IsInit) return;

        try
        {
            IsInit = true;

            //Your hooks go here
            Logger.LogMessage("Started Telemetry logger");
            On.Player.Update += HookPlayerUpdate;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private void HookPlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu); //Always call original code, either before or after your code, depending on what you need to achieve

        Player.InputPackage input = self.input[0];
        if (input.spec)
        {
            this.special_pressed = true;
        }
        else
        {
            if (this.special_pressed){
                this.special_pressed = false;
                this.should_log = !this.should_log;
                Logger.LogDebug("Toggled logging");
                telemetryfile.WriteLine("Toggled logging");
            }
        }

        if (this.should_log)    
        { var connections = new object[self.bodyChunkConnections.Length];
            for (int i = 0; i < self.bodyChunkConnections.Length; i++)
            {var conn = self.bodyChunkConnections[i];
                connections[i] = new
                {
                    chunk1_pos_x = conn.chunk1.pos.x,
                    chunk1_pos_y = conn.chunk1.pos.y,
                    chunk1_vel_x = conn.chunk1.vel.x,
                    chunk1_vel_y = conn.chunk1.vel.y,
                    chunk2_pos_x = conn.chunk2.pos.x,
                    chunk2_pos_y = conn.chunk2.pos.y,
                    chunk2_vel_x = conn.chunk2.vel.x,
                    chunk2_vel_y = conn.chunk2.vel.y,
                    distance = conn.distance,
                    elasticity = conn.elasticity,
                    weight_symmetry = conn.weightSymmetry,
                    type = conn.type.ToString(),
                    active = conn.active,
                };
            }
            var frameData = new
            {
                frame = UnityEngine.Time.frameCount,
                // Input data
                input_x = input.x,
                input_y = input.y,
                input_jump = input.jmp,
                // Position/velocity
                main_bod_pos_x = self.mainBodyChunk.pos.x,
                main_bod_pos_y = self.mainBodyChunk.pos.y,
                main_bod_vel_x = self.mainBodyChunk.vel.x,
                main_bod_vel_y = self.mainBodyChunk.vel.y,
                // Animation Data
                body_chunk_count = self.bodyChunks.Length,
                anim = self.animation,
                body_mode = self.bodyMode,
                can_wall_jump = self.canWallJump,
                landing_delay = self.landingDelay,
                ledge_grab_counter = self.ledgeGrabCounter,
                lb_frames_off_ground = self.lowerBodyFramesOffGround,
                lb_frames_on_ground = self.lowerBodyFramesOnGround,
                ub_frames_off_ground = self.upperBodyFramesOffGround,
                ub_frames_on_ground = self.upperBodyFramesOnGround,
                slide_counter = self.slideCounter,
                want_to_jump = self.wantToJump,
                wiggles = self.wiggle,
                body_connections = connections,
            };
        telemetryfile.WriteLine(JsonConvert.SerializeObject(frameData)); 
        }
    }
}
     
