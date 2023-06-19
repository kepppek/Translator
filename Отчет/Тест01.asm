format PE console
entry address0
include 'INCLUDE/win32a.inc'
section '.data' data readable writable
      db '%d',10,0
      a dd ?
      n dd ?
      res dd ?
section '.code' code readable executable
      address0:
      push dword 2
      ;   a   
      pop [a]
      push dword 5
      ;   n   
      pop [n]
      push [a]
      ;   res   
      pop [res]
      invoke printf,401000h,[res]
      call [getch]
section '.idata' import data readable
   library msvcrt,'msvcrt.dll'
   import msvcrt,\
      printf,'printf',\
      getch,'_getch'