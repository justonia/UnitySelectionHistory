
#InstallKeybdHook
#SingleInstance force
SetTitleMatchMode 2
SendMode Input

#IfWinActive, ahk_class UnityContainerWndClass

; Mouse back button
XButton1::Send ^+z

; Mouse forward button
XButton2::Send ^+y

#ifWinActive
