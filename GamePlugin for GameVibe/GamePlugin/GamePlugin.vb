﻿Option Explicit On
Option Strict On

Imports Game_PluginAPI
Imports System.Runtime.InteropServices
Imports System.Net.Sockets
Imports System.Net
Imports System.Windows
Imports System.Windows.Media.Media3D
Imports System.Threading

Public Class GamePlugin
    Implements IPlugin_Game
    '//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '///                            SimTools Plugin - Edit the Setting below to provide support for your favorite game!                             ///
    '//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '//////////////////////////////////////////////// 
    '/// Per Game Settings - Change for Each Game ///
    '////////////////////////////////////////////////
    Private Const _PluginAuthorsName As String = "yobuddy, value1"
    Private Const _GameName As String = "BeamNG" 'GameName (Must Be Unique!) - the displayed Name. (DLL_Name without spaces ==> "LiveforSpeed.dll")
    Private Const _ProcessName As String = "BeamNG.drive,BeamNG.drive.x64" 'Process_Name without the (".exe") for this game
    Private Const _Port As String = "4444"  'Your Sending/Recieving UDP Port for this game
    Private Const _RequiresPatchingPath As Boolean = False 'do we need the game exe path for patching? (must be true if _RequiresSecondCheck = True)
    Private Const _RequiresSecondCheck As Boolean = True 'Use when games have the same _ProcessName. (all plugins with the same _ProcessName must be updated)
    Private Const _PluginOptions As String = "" ' Reserved For Future Use - No Change Needed (Leave Blank)
    '////////////////////////////////////////////////
    '///           Memory Map Variables           ///
    '////////////////////////////////////////////////
    Private Const _Enable_MemoryMap As Boolean = False 'Is a MemoryMap file Required for this game?
    Private Const _MMF_Name As String = "NULL" 'Set if using a Memory Map File - EXAMPLE("$gtr2$")
    '////////////////////////////////////////////////
    '///           MemoryHook Variables           ///
    '////////////////////////////////////////////////   
    Private Const _Enable_MemoryHook As Boolean = False 'Is a Memory Hook Required for this game? 
    Private Const _MemHook_Roll As UInteger = 0 'Not Used = 0
    Private Const _MemHook_Pitch As UInteger = 0
    Private Const _MemHook_Heave As UInteger = 0
    Private Const _MemHook_Yaw As UInteger = 0
    Private Const _MemHook_Sway As UInteger = 0
    Private Const _MemHook_Surge As UInteger = 0
    Private Const _MemHook_Extra1 As UInteger = 0
    Private Const _MemHook_Extra2 As UInteger = 0
    Private Const _MemHook_Extra3 As UInteger = 0
    '////////////////////////////////////////////////
    '///    DOFs Used for Output for this Game    ///
    '////////////////////////////////////////////////
    Private Const _DOF_Support_Roll As Boolean = True
    Private Const _DOF_Support_Pitch As Boolean = True
    Private Const _DOF_Support_Heave As Boolean = True
    Private Const _DOF_Support_Yaw As Boolean = True
    Private Const _DOF_Support_Sway As Boolean = True
    Private Const _DOF_Support_Surge As Boolean = True
    Private Const _DOF_Support_Extra1 As String = "Traction Loss" 'Blank = False
    Private Const _DOF_Support_Extra2 As String = "" '"" = Not Used
    Private Const _DOF_Support_Extra3 As String = "" 'ADD THE FORCE NAME HERE
    '/////////////////////////////////////////////////
    '///       GameDash - Dash Board Support       ///
    '/////////////////////////////////////////////////
    Private Const _Enable_DashBoard As Boolean = True 'Enable the DashBoard Output System?
    'EXAMPLES OF DASH OUTPUT (all variables are strings) - 20 DASH OUTPUTS MAX!!!
    'Use the Variables with either Process_PacketRecieved, Process_MemoryHook or Process_MemoryMap.
    'Variable = (*Action, Value) as String - (The *Action variable can be anything)
    'Basic Dash Support should include at the minimum -  SPEED, RPM and GEAR
    'Dash_1_Output = "Speed," & OUTPUT_VALUE_HERE.ToString
    'Dash_2_Output = "Rpm," & OUTPUT_VALUE_HERE.ToString
    'Dash_2_Output = "Gear," & OUTPUT_VALUE_HERE.ToString
    '...
    'Dash_20_Output = "Engine Temp," & OUTPUT_VALUE_HERE.ToString
    '/////////////////////////////////////////////////
    '///           GameVibe Support              ///
    '/////////////////////////////////////////////////
    Private Const _Enable_GameVibe As Boolean = True 'Enable the GameVibe Output System?
    'EXAMPLES OF GameVibe OUTPUT (all variables are strings) - 9 GameVibe OUTPUTS MAX!!!
    'Use the Variables with either Process_PacketRecieved, Process_MemoryHook or Process_MemoryMap.
    'Variable = (*Action, Value) as String - (The *Action" variable can ONLY be: Rpm, Heave, Sway, Surge, FR, FL, RR, RL, Turbo)
    'Basic GameVibe Support should include at the minimum -  Rpm
    'Vibe_1_Output = "Rpm," & OUTPUT_VALUE_HERE.ToString
    'Vibe_2_Output = "Heave," & OUTPUT_VALUE_HERE.ToString
    'Vibe_3_Output = "Sway," & OUTPUT_VALUE_HERE.ToString
    'Vibe_4_Output = "Surge," & OUTPUT_VALUE_HERE.ToString
    '...
    'Vibe_9_Output = "Gear Shift," & OUTPUT_VALUE_HERE.ToString
    '/////////////////////////////////////////////////

    'Used by GameManager when the Game Starts.
    Public Sub GameStart() Implements IPlugin_Game.GameStart
        'DO SOMETHING HERE AT GAME START!

        'only collect dash data if its used
        IsUsingDash = IsDashUsed()
        If IsUsingDash = True Then
            DashClient = New UdpClient(4445) 'port
            DashThreadReceive = New Thread(AddressOf ProcessDash)
            DashThreadReceive.IsBackground = True
            DashThreadReceive.Start()
        End If
    End Sub

    'Used by GameManager when the Game Stops.
    Public Sub GameStop() Implements IPlugin_Game.GameEnd
        'DO SOMETHING HERE AT GAME STOPS!

        If IsUsingDash = True Then
            On Error Resume Next
            IsUsingDash = False
            DashThreadReceive.Abort()
            DashClient.Close() 'may not work twice in a row if its closed??         
        End If
    End Sub

    'Used by GameManager to Process a MemoryHook.
    Public Sub Process_MemoryHook() Implements IPlugin_Game.Process_MemoryHook
        'DO SOMETHING HERE AT GAME START!        
    End Sub

    'Used by GameManager to Process a MemoryMap.
    Public Sub Process_MemoryMap() Implements IPlugin_Game.Process_MemoryMap
        'DO SOMETHING HERE AT GAME START!
    End Sub

    'Used by GameEngine to Process Incoming UDP Packets.
    Public Sub Process_PacketRecieved(Text As String) Implements IPlugin_Game.Process_PacketRecieved
        Try
            'Convert string to byte and copy to byte array
            Dim ByteArray() As Byte = System.Text.Encoding.Default.GetBytes(Text)
            'Create Gchandle instance and pin variable required
            Dim MyGC As GCHandle = GCHandle.Alloc(MyOutsim_Internal, GCHandleType.Pinned)
            'get address of variable in pointer variable
            Dim AddofLongValue As IntPtr = MyGC.AddrOfPinnedObject()
            'Copy the memory space to my GCHandle
            Marshal.Copy(ByteArray, 0, AddofLongValue, ByteArray.Length)
            'Direct Cast myGC to my Outsim Object
            MyOutsim_Internal = DirectCast(MyGC.Target, motionSim_t)
            'Free GChandle to avoid memory leaks
            MyGC.Free()

            'Get Proper Data out of UDP Packet
            With MyOutsim_Internal
                Roll_Output = (.rollPos * 180 / 3.14159)
                Pitch_Output = (.pitchPos * 180 / 3.14159)
                Yaw_Output = (.yawPos * 180 / 3.14159)
                Sway_Output = .accX
                Surge_Output = .accY
                Heave_Output = .accZ

                Dim forward As Vector3D = New Vector3D(.forwardVecX, .forwardVecY, .forwardVecZ)
                Dim up As Vector3D = New Vector3D(.upVecX, .upVecY, .upVecZ)
                Dim right As Vector3D = Vector3D.CrossProduct(forward, up)

                Dim tracLoss As Double = -(.velX * right.X + .velY * right.Y + .velZ * right.Z)

                Extra1_Output = tracLoss

                Vibe_4_Output = "Collision L/R," & Sway_Output.ToString
                Vibe_5_Output = "Collision F/B," & Surge_Output.ToString
                Vibe_6_Output = "Road Detail," & Heave_Output.ToString
            End With
        Catch ex As Exception
        End Try
    End Sub

    'Used by GameManager to Patch a Game.
    Public Function PatchGame(ByVal MyPath As String, ByVal MyIp As String) As Boolean Implements IPlugin_Game.PatchGame
        'Change as Needed

        'If game is already patched - Unpatch first to be safe
        UnPatch(MyPath)

        'Patch Game
        MsgBox("BeamNG patched.", MsgBoxStyle.OkOnly, "Patching info")
        Return True

    End Function

    'Used by GameManager to UnPatch a Game.
    Public Sub UnPatchGame(MyPath As String) Implements IPlugin_Game.UnPatchGame
        'Change as Needed
        UnPatch(MyPath)
        MsgBox("Patch Uninstalled!", MsgBoxStyle.OkOnly, "Patching info")
    End Sub

    'Used by GameManager to UnPatch a Game.
    Private Sub UnPatch(MyPath As String)
        'Restore backup file if found (remove patch)
    End Sub

    'Tells the User where to patch the game 
    Public Sub PatchPathInfo() Implements IPlugin_Game.PatchPathInfo
        'Tell the User where to patch the game 
        'MsgBox("Please Select the Live for Speed's Installation Directory.", MsgBoxStyle.OkOnly, "Patching info")
    End Sub

    'Used by GameManager to Validate a Path befors Patching.
    Public Function ValidatePatchPath(MyPath As String) As Boolean Implements IPlugin_Game.ValidatePatchPath
        'insert a simple validation of the patching path - let the user know he got it right
        Return True
    End Function


    '//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '///                                                 PLACE EXTRA NEEDED CODE/FUNCTIONS HERE                                                     ///
    '//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#Region "///// - EXTRA CODE/FUNCTIONS USED FOR THIS PLUGIN - /////"
    Private IsUsingDash As Boolean = False

    'File Editor - Replaces a line in a txt/cfg file
    Private Sub MyFileEditor(FileName As String, LinetoEdit As String, StingReplacment As String)
        Dim lines As New List(Of String)
        Dim x As Integer = 0
        Using sr As New System.IO.StreamReader(FileName)
            While Not sr.EndOfStream
                lines.Add(sr.ReadLine)
            End While
        End Using

        For Each line As String In lines
            If line.Contains(LinetoEdit) Then
                lines.RemoveAt(x)
                lines.Insert(x, StingReplacment)
                Exit For 'must exit as we changed the iteration   
            End If
            x = x + 1
        Next

        Using sw As New System.IO.StreamWriter(FileName)
            For Each line As String In lines
                sw.WriteLine(line)
            Next
        End Using
        lines.Clear()
    End Sub

    'Is the DashOutput Being Used? - returns True or false
    Private Function IsDashUsed() As Boolean
        Dim SavePath_Dash As String = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\SimTools\GameManager\UseDash.cfg"
        Dim SavePath_Vibe As String = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\SimTools\GameManager\UseVibe.cfg"
        If IO.File.Exists(SavePath_Dash) = True Or IO.File.Exists(SavePath_Vibe) = True Then
            Return True
        Else
            Return False
        End If
    End Function

    Private DashClient As UdpClient
    Public DashRemoteIpEndPoint As New IPEndPoint(System.Net.IPAddress.Any, 4124) 'any ip we dont care
    Public DashThreadReceive As System.Threading.Thread
    Public Sub ProcessDash()
        Try
            Dim OutGaugeData As New outgauge_t
            Dim ptr As New GCHandle
            Dim OutGaugeBytes() As Byte = DashClient.Receive(DashRemoteIpEndPoint)
            Dim OutGaugeSize As Integer = Marshal.SizeOf(OutGaugeData)
            'Dim CodemasterSize As Integer = 68
            ptr = GCHandle.Alloc(OutGaugeBytes, GCHandleType.Pinned)
            MyOutGaudge_Internal = CType(Marshal.PtrToStructure(ptr.AddrOfPinnedObject, GetType(outgauge_t)), outgauge_t)
            ptr.Free() '<< make sure you free the gc or you get memory problems

            'Get Proper Data out of UDP Packet
            With MyOutGaudge_Internal
                'Dash
                'Dash_1_Output = "Speed [m/s]," & Int(.Speed + 0.5) 'original 
                Dash_1_Output = "Speed [m/s]," & .speed  'testing
                Dash_2_Output = "RPM," & Int(.rpm + 0.5)
                Dash_3_Output = "Gear (-1 = Reverse)," & .gear - 1
                Dash_4_Output = "EngineWaterTemp [°C]," & .engTemp
                Dash_5_Output = "EngineOilTemp [°C]," & .oilTemp
                Dash_6_Output = "Fuel [%]," & .fuel
                Dash_7_Output = "Engine Overheating," & (.flags And CInt(2 ^ DL_x.DL_OILWARN))
                Dash_8_Output = "Turbo [bar]," & .turbo
                Dash_9_Output = "Headlights," & (.flags And CInt(2 ^ DL_x.DL_FULLBEAM))
                Dash_10_Output = "ShiftUp," & (.flags And CInt(2 ^ DL_x.DL_SHIFT))  ' shift up light
                Dash_11_Output = "Oil Pressure [bar]," & .oilPressure
                'Dash_12_Output = " xxx," & 
                Dash_13_Output = "InPit," & (.flags And CInt(2 ^ DL_x.DL_PITSPEED))
                Dash_14_Output = "Left turn signal," & (.flags And CInt(2 ^ DL_x.DL_SIGNAL_L))
                Dash_15_Output = "Right turn signal," & (.flags And CInt(2 ^ DL_x.DL_SIGNAL_R))
                Dash_16_Output = "Shared turn signal," & (.flags And CInt(2 ^ DL_x.DL_SIGNAL_ANY))
                Dash_17_Output = "Battery warning," & (.flags And CInt(2 ^ DL_x.DL_BATTERY))
                Dash_18_Output = "ABS," & (.flags And CInt(2 ^ DL_x.DL_ABS))
                Dash_19_Output = "Handbrake," & (.flags And CInt(2 ^ DL_x.DL_HANDBRAKE))

                'Vibe
                Vibe_1_Output = "RPM," & (Int(.rpm + 0.5).ToString)
                Vibe_2_Output = "Gear Shift," & (.gear - 1).ToString
                Vibe_3_Output = "Turbo," & .turbo.ToString
            End With
        Catch ex As Exception
        End Try

        'Startup a new thread listener
        If IsUsingDash = True Then
            DashThreadReceive = New System.Threading.Thread(AddressOf ProcessDash)
            DashThreadReceive.IsBackground = True
            DashThreadReceive.Start()
        End If
    End Sub

    'Used by GameEngine to Process Incoming UDP Packets.
    Private MyOutsim_Internal As New motionSim_t
    Private Structure motionSim_t
        'Magic to check if packet is actually useful, fixed value of "BNG1"
        Public magic As Int32

        'World position of the car
        Public posX As Single
        Public posY As Single
        Public posZ As Single

        'Velocity of the car
        Public velX As Single
        Public velY As Single
        Public velZ As Single

        'Acceleration of the car, gravity not included
        Public accX As Single
        Public accY As Single
        Public accZ As Single

        'Vector components of a vector pointing "up" relative to the car
        Public upVecX As Single
        Public upVecY As Single
        Public upVecZ As Single

        'Roll, pitch and yaw positions of the car
        Public rollPos As Single
        Public pitchPos As Single
        Public yawPos As Single

        'Roll, pitch and yaw "velocities" of the car
        Public rollRate As Single
        Public pitchRate As Single
        Public yawRate As Single

        'Roll, pitch and yaw "accelerations" of the car
        Public rollAcc As Single
        Public pitchAcc As Single
        Public yawAcc As Single

        'added ck
        Public forwardVecX As Single
        Public forwardVecY As Single
        Public forwardVecZ As Single


    End Structure


    Private MyOutGaudge_Internal As New outgauge_t

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Private Structure outgauge_t
        Public time As UInt32        ' time in milliseconds (to check order)
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=4)>
        Public car As String         ' Car name
        Public flags As UInt16       ' Info (see OG_x below)
        Public gear As Byte          ' Reverse:0, Neutral:1, First:2...
        Public plid As Byte          ' Unique ID of viewed player (0 = none)
        Public speed As Single       ' M/S
        Public rpm As Single         ' RPM
        Public turbo As Single       ' BAR
        Public engTemp As Single     ' C
        Public fuel As Single        ' 0 to 1
        Public oilPressure As Single ' BAR
        Public oilTemp As Single     ' C
        Public dashLights As UInt32  ' Dash lights available (see DL_x below)
        Public showLights As UInt32  ' Dash lights currently switched on
        Public throttle As Single    ' 0 to 1
        Public brake As Single       ' 0 to 1
        Public clutch As Single      ' 0 to 1
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=16)>
        Public display1 As String    ' Usually Fuel
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=16)>
        Public display2 As String    ' Usually Settings
        Public id As Int32           ' optional - only if OutGauge ID is specified
    End Structure


    Enum DL_x
        DL_SHIFT ' 			bit 0	- shift light
        DL_FULLBEAM '		bit 1	- full beam
        DL_HANDBRAKE '		bit 2	- handbrake
        DL_PITSPEED ' 		bit 3	- pit speed limiter
        DL_TC '				bit 4	- TC active or switched off
        DL_SIGNAL_L '		bit 5	- left turn signal
        DL_SIGNAL_R '		bit 6	- right turn signal
        DL_SIGNAL_ANY '		bit 7	- shared turn signal
        DL_OILWARN '		bit 8	- oil pressure warning
        DL_BATTERY '		bit 9	- battery warning
        DL_ABS '			bit 10	- ABS active or switched off
        DL_SPARE '			bit 11  - spare
        DL_NUM '            bit 12  - ?
    End Enum
#End Region


    '//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '///                                                        DO NOT EDIT BELOW HERE!!!                                                           ///
    '//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#Region "///// BUILT IN METHODS FOR SIMTOOLS - DO NOT CHANGE /////"
    'Output Dash Vars
    Public Dash_1_Output As String = ""
    Public Dash_2_Output As String = ""
    Public Dash_3_Output As String = ""
    Public Dash_4_Output As String = ""
    Public Dash_5_Output As String = ""
    Public Dash_6_Output As String = ""
    Public Dash_7_Output As String = ""
    Public Dash_8_Output As String = ""
    Public Dash_9_Output As String = ""
    Public Dash_10_Output As String = ""
    Public Dash_11_Output As String = ""
    Public Dash_12_Output As String = ""
    Public Dash_13_Output As String = ""
    Public Dash_14_Output As String = ""
    Public Dash_15_Output As String = ""
    Public Dash_16_Output As String = ""
    Public Dash_17_Output As String = ""
    Public Dash_18_Output As String = ""
    Public Dash_19_Output As String = ""
    Public Dash_20_Output As String = ""

    'Output Vars
    Public Roll_Output As Double = 0
    Public Pitch_Output As Double = 0
    Public Heave_Output As Double = 0
    Public Yaw_Output As Double = 0
    Public Sway_Output As Double = 0
    Public Surge_Output As Double = 0
    Public Extra1_Output As Double = 0
    Public Extra2_Output As Double = 0
    Public Extra3_Output As Double = 0

    'MemHook Vars
    Public Roll_MemHook As Double = 0
    Public Pitch_MemHook As Double = 0
    Public Heave_MemHook As Double = 0
    Public Yaw_MemHook As Double = 0
    Public Sway_MemHook As Double = 0
    Public Surge_MemHook As Double = 0
    Public Extra1_MemHook As Double = 0
    Public Extra2_MemHook As Double = 0
    Public Extra3_MemHook As Double = 0

    'MemMap Vars
    Public Roll_MemMap As Double = 0
    Public Pitch_MemMap As Double = 0
    Public Heave_MemMap As Double = 0
    Public Yaw_MemMap As Double = 0
    Public Sway_MemMap As Double = 0
    Public Surge_MemMap As Double = 0
    Public Extra1_MemMap As Double = 0
    Public Extra2_MemMap As Double = 0
    Public Extra3_MemMap As Double = 0

    'GameVibe Vars
    Public Vibe_1_Output As String = ""
    Public Vibe_2_Output As String = ""
    Public Vibe_3_Output As String = ""
    Public Vibe_4_Output As String = ""
    Public Vibe_5_Output As String = ""
    Public Vibe_6_Output As String = ""
    Public Vibe_7_Output As String = ""
    Public Vibe_8_Output As String = ""
    Public Vibe_9_Output As String = ""

    Public Function Get_PluginVersion() As String Implements IPlugin_Game.Get_PluginVersion
        Return System.Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString
    End Function

    Public Function GetDOFsUsed() As String Implements IPlugin_Game.GetDOFsUsed
        'Return DOF's Used (Roll,Pitch,Heave,Yaw,Sway,Surge)
        Return (_DOF_Support_Roll.ToString & "," & _DOF_Support_Pitch.ToString & "," & _DOF_Support_Heave.ToString & "," & _DOF_Support_Yaw.ToString & "," & _DOF_Support_Sway.ToString & "," & _DOF_Support_Surge.ToString & "," & _DOF_Support_Extra1.ToString & "," & _DOF_Support_Extra2.ToString & "," & _DOF_Support_Extra3.ToString)
    End Function

    Public Sub ResetDOFVars() Implements IPlugin_Game.ResetDOFVars
        Roll_Output = 0
        Pitch_Output = 0
        Heave_Output = 0
        Yaw_Output = 0
        Sway_Output = 0
        Surge_Output = 0
        Extra1_Output = 0
        Extra2_Output = 0
        Extra3_Output = 0
    End Sub

    Public Sub ResetMapVars() Implements IPlugin_Game.ResetMapVars
        Roll_MemMap = 0
        Pitch_MemMap = 0
        Heave_MemMap = 0
        Yaw_MemMap = 0
        Sway_MemMap = 0
        Surge_MemMap = 0
        Extra1_MemMap = 0
        Extra2_MemMap = 0
        Extra3_MemMap = 0
    End Sub

    Public Sub ResetHookVars() Implements IPlugin_Game.ResetHookVars
        Roll_MemHook = 0
        Pitch_MemHook = 0
        Heave_MemHook = 0
        Yaw_MemHook = 0
        Sway_MemHook = 0
        Surge_MemHook = 0
        Extra1_MemHook = 0
        Extra2_MemHook = 0
        Extra3_MemHook = 0
    End Sub

    Public Sub ResetDashVars() Implements IPlugin_Game.ResetDashVars
        Dash_1_Output = ""
        Dash_2_Output = ""
        Dash_3_Output = ""
        Dash_4_Output = ""
        Dash_5_Output = ""
        Dash_6_Output = ""
        Dash_7_Output = ""
        Dash_8_Output = ""
        Dash_9_Output = ""
        Dash_10_Output = ""
        Dash_11_Output = ""
        Dash_12_Output = ""
        Dash_13_Output = ""
        Dash_14_Output = ""
        Dash_15_Output = ""
        Dash_16_Output = ""
        Dash_17_Output = ""
        Dash_18_Output = ""
        Dash_19_Output = ""
        Dash_20_Output = ""
    End Sub

    Public Sub ResetVibeVars() Implements IPlugin_Game.ResetVibeVars
        Vibe_1_Output = ""
        Vibe_2_Output = ""
        Vibe_3_Output = ""
        Vibe_4_Output = ""
        Vibe_5_Output = ""
        Vibe_6_Output = ""
        Vibe_7_Output = ""
        Vibe_8_Output = ""
        Vibe_9_Output = ""
    End Sub

    Public ReadOnly Property PluginAuthorsName() As String Implements IPlugin_Game.PluginAuthorsName
        Get
            Return _PluginAuthorsName
        End Get
    End Property

    Public ReadOnly Property Name() As String Implements IPlugin_Game.GameName
        Get
            Return _GameName
        End Get
    End Property

    Public ReadOnly Property ProcessName() As String Implements IPlugin_Game.ProcessName
        Get
            Return _ProcessName
        End Get
    End Property

    Public ReadOnly Property Port() As String Implements IPlugin_Game.Port
        Get
            Return _Port
        End Get
    End Property

    Public ReadOnly Property Enable_MemoryMap() As Boolean Implements IPlugin_Game.Enable_MemoryMap
        Get
            Return _Enable_MemoryMap
        End Get
    End Property

    Public ReadOnly Property Enable_MemoryHook() As Boolean Implements IPlugin_Game.Enable_MemoryHook
        Get
            Return _Enable_MemoryHook
        End Get
    End Property

    Public ReadOnly Property RequiresPatchingPath() As Boolean Implements IPlugin_Game.RequiresPatchingPath
        Get
            Return _RequiresPatchingPath
        End Get
    End Property

    Public ReadOnly Property RequiresSecondCheck() As Boolean Implements IPlugin_Game.RequiresSecondCheck
        Get
            Return _RequiresSecondCheck
        End Get
    End Property

    Public ReadOnly Property Enable_DashBoard() As Boolean Implements IPlugin_Game.Enable_DashBoard
        Get
            Return _Enable_DashBoard
        End Get
    End Property

    Public ReadOnly Property Enable_GameVibe() As Boolean Implements IPlugin_Game.Enable_GameVibe
        Get
            Return _Enable_GameVibe
        End Get
    End Property

    Public Function Get_RollOutput() As Double Implements IPlugin_Game.Get_RollOutput
        Return Roll_Output
    End Function

    Public Function Get_PitchOutput() As Double Implements IPlugin_Game.Get_PitchOutput
        Return Pitch_Output
    End Function

    Public Function Get_HeaveOutput() As Double Implements IPlugin_Game.Get_HeaveOutput
        Return Heave_Output
    End Function

    Public Function Get_YawOutput() As Double Implements IPlugin_Game.Get_YawOutput
        Return Yaw_Output
    End Function

    Public Function Get_SwayOutput() As Double Implements IPlugin_Game.Get_SwayOutput
        Return Sway_Output
    End Function

    Public Function Get_SurgeOutput() As Double Implements IPlugin_Game.Get_SurgeOutput
        Return Surge_Output
    End Function

    Public Function Get_Extra1Output() As Double Implements IPlugin_Game.Get_Extra1Output
        Return Extra1_Output
    End Function

    Public Function Get_Extra2Output() As Double Implements IPlugin_Game.Get_Extra2Output
        Return Extra2_Output
    End Function

    Public Function Get_Extra3Output() As Double Implements IPlugin_Game.Get_Extra3Output
        Return Extra3_Output
    End Function

    Public Function Get_Dash_1_Output() As String Implements IPlugin_Game.Get_Dash1_Output
        Return Dash_1_Output
    End Function

    Public Function Get_Dash_2_Output() As String Implements IPlugin_Game.Get_Dash2_Output
        Return Dash_2_Output
    End Function

    Public Function Get_Dash_3_Output() As String Implements IPlugin_Game.Get_Dash3_Output
        Return Dash_3_Output
    End Function

    Public Function Get_Dash_4_Output() As String Implements IPlugin_Game.Get_Dash4_Output
        Return Dash_4_Output
    End Function

    Public Function Get_Dash_5_Output() As String Implements IPlugin_Game.Get_Dash5_Output
        Return Dash_5_Output
    End Function

    Public Function Get_Dash_6_Output() As String Implements IPlugin_Game.Get_Dash6_Output
        Return Dash_6_Output
    End Function

    Public Function Get_Dash_7_Output() As String Implements IPlugin_Game.Get_Dash7_Output
        Return Dash_7_Output
    End Function

    Public Function Get_Dash_8_Output() As String Implements IPlugin_Game.Get_Dash8_Output
        Return Dash_8_Output
    End Function

    Public Function Get_Dash_9_Output() As String Implements IPlugin_Game.Get_Dash9_Output
        Return Dash_9_Output
    End Function

    Public Function Get_Dash_10_Output() As String Implements IPlugin_Game.Get_Dash10_Output
        Return Dash_10_Output
    End Function

    Public Function Get_Dash_11_Output() As String Implements IPlugin_Game.Get_Dash11_Output
        Return Dash_11_Output
    End Function

    Public Function Get_Dash_12_Output() As String Implements IPlugin_Game.Get_Dash12_Output
        Return Dash_12_Output
    End Function

    Public Function Get_Dash_13_Output() As String Implements IPlugin_Game.Get_Dash13_Output
        Return Dash_13_Output
    End Function

    Public Function Get_Dash_14_Output() As String Implements IPlugin_Game.Get_Dash14_Output
        Return Dash_14_Output
    End Function

    Public Function Get_Dash_15_Output() As String Implements IPlugin_Game.Get_Dash15_Output
        Return Dash_15_Output
    End Function

    Public Function Get_Dash_16_Output() As String Implements IPlugin_Game.Get_Dash16_Output
        Return Dash_16_Output
    End Function

    Public Function Get_Dash_17_Output() As String Implements IPlugin_Game.Get_Dash17_Output
        Return Dash_17_Output
    End Function

    Public Function Get_Dash_18_Output() As String Implements IPlugin_Game.Get_Dash18_Output
        Return Dash_18_Output
    End Function

    Public Function Get_Dash_19_Output() As String Implements IPlugin_Game.Get_Dash19_Output
        Return Dash_19_Output
    End Function

    Public Function Get_Dash_20_Output() As String Implements IPlugin_Game.Get_Dash20_Output
        Return Dash_20_Output
    End Function

    Public Function Get_RollMemHook() As Double Implements IPlugin_Game.Get_RollMemHook
        Return Roll_MemHook
    End Function

    Public Function Get_PitchMemHook() As Double Implements IPlugin_Game.Get_PitchMemHook
        Return Pitch_MemHook
    End Function

    Public Function Get_HeaveMemHook() As Double Implements IPlugin_Game.Get_HeaveMemHook
        Return Heave_MemHook
    End Function

    Public Function Get_YawMemHook() As Double Implements IPlugin_Game.Get_YawMemHook
        Return Yaw_MemHook
    End Function

    Public Function Get_SwayMemHook() As Double Implements IPlugin_Game.Get_SwayMemHook
        Return Sway_MemHook
    End Function

    Public Function Get_SurgeMemHook() As Double Implements IPlugin_Game.Get_SurgeMemHook
        Return Surge_MemHook
    End Function

    Public Function Get_Extra1MemHook() As Double Implements IPlugin_Game.Get_Extra1MemHook
        Return Extra1_MemHook
    End Function

    Public Function Get_Extra2MemHook() As Double Implements IPlugin_Game.Get_Extra2MemHook
        Return Extra2_MemHook
    End Function

    Public Function Get_Extra3MemHook() As Double Implements IPlugin_Game.Get_Extra3MemHook
        Return Extra3_MemHook
    End Function

    Public Function Get_RollMemMap() As Double Implements IPlugin_Game.Get_RollMemMap
        Return Roll_MemMap
    End Function

    Public Function Get_PitchMemMap() As Double Implements IPlugin_Game.Get_PitchMemMap
        Return Pitch_MemMap
    End Function

    Public Function Get_HeaveMemMap() As Double Implements IPlugin_Game.Get_HeaveMemMap
        Return Heave_MemMap
    End Function

    Public Function Get_YawMemMap() As Double Implements IPlugin_Game.Get_YawMemMap
        Return Yaw_MemMap
    End Function

    Public Function Get_SwayMemMap() As Double Implements IPlugin_Game.Get_SwayMemMap
        Return Sway_MemMap
    End Function

    Public Function Get_SurgeMemMap() As Double Implements IPlugin_Game.Get_SurgeMemMap
        Return Surge_MemMap
    End Function

    Public Function Get_Extra1MemMap() As Double Implements IPlugin_Game.Get_Extra1MemMap
        Return Extra1_MemMap
    End Function

    Public Function Get_Extra2MemMap() As Double Implements IPlugin_Game.Get_Extra2MemMap
        Return Extra2_MemMap
    End Function

    Public Function Get_Extra3MemMap() As Double Implements IPlugin_Game.Get_Extra3MemMap
        Return Extra3_MemMap
    End Function

    Public Function Get_Vibe1_Output() As String Implements IPlugin_Game.Get_Vibe1_Output
        Return Vibe_1_Output
    End Function

    Public Function Get_Vibe2_Output() As String Implements IPlugin_Game.Get_Vibe2_Output
        Return Vibe_2_Output
    End Function

    Public Function Get_Vibe3_Output() As String Implements IPlugin_Game.Get_Vibe3_Output
        Return Vibe_3_Output
    End Function

    Public Function Get_Vibe4_Output() As String Implements IPlugin_Game.Get_Vibe4_Output
        Return Vibe_4_Output
    End Function

    Public Function Get_Vibe5_Output() As String Implements IPlugin_Game.Get_Vibe5_Output
        Return Vibe_5_Output
    End Function

    Public Function Get_Vibe6_Output() As String Implements IPlugin_Game.Get_Vibe6_Output
        Return Vibe_6_Output
    End Function

    Public Function Get_Vibe7_Output() As String Implements IPlugin_Game.Get_Vibe7_Output
        Return Vibe_7_Output
    End Function

    Public Function Get_Vibe8_Output() As String Implements IPlugin_Game.Get_Vibe8_Output
        Return Vibe_8_Output
    End Function

    Public Function Get_Vibe9_Output() As String Implements IPlugin_Game.Get_Vibe9_Output
        Return Vibe_9_Output
    End Function

    Public ReadOnly Property PluginOptions() As String Implements IPlugin_Game.PluginOptions
        Get
            Return _PluginOptions
        End Get
    End Property
#End Region
End Class