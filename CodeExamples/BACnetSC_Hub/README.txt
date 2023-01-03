BACnet/SC Hub is a questionable (partially respecting the protocol) but working implementation of HUB service, but to experiment PKI.
This application can be used also for Wireshark capture of ciphered channels pushed in an unciphered channel : for programmer only, see code.

For Windows 10 see trunk\Docs\ActivateTLS1.3 On Win10.reg

The process of certificate validation is coded by a no expert (me) and need to be greatly modified ... finaly that's why I've do it.
Please help : fchaxel@free.fr

PKI directory :
  
   --- own
   --- trusted
   --- rejected
   --- issuers
   --- certdatabase
              |
               --- p12 With private Key


# In "certdatabase" directory the file xca.xdb is an XCA database (https://hohnstaedt.de/xca/index.php) with keys & certificates.
The database is not encrypted nor password protected. A real one should be !
All exported certificates are present. P12 files (cert + private Key without password) are in the "p12...." directory.
Certviewer is a C# application to show certificate. Run in WinForm and command line mode : https://sourceforge.net/projects/x509-certificate-viewer/
P12, pfx maybe other files cannot be open by a click. So this tool is here for that and to navigate in the Cert chain if exist.
Cetificate files can be Drop on the Form or you can paste a valide URL (https, wss) using the popupMenu.
Don't work on Linux for unknown reason. 

Certificates tree structure is :

    CA1
      |
      ---- CA11  ------------- CA12
              |                          |
               --- FakeNode        --- Hub
                                            --- Node
                                            --- Node2
                                            --- Node3 (outdated)

and also a tree structure coming from the BACnet/SC Reference Stack (https://sourceforge.net/projects/bacnet-sc-reference-stack/) :

    Bar_Signing_CA
      |
       ---- TestNode
       ---- TestHub
          
# In "own" directory the Hub application certificate, Hub.p12, can be found (with private key not password protected).

# In "rejected" you can place certificates not accepted by the application (endUser or CA12 or CA1-CA12 ...). 
  By default Node2 and CA11 are inside. So using Node2 will be rejected, FakeNode too.

# In "trusted" you can place certificates accepted by the application (endUser or CA12 or CA1-CA12 ...). 
   By default CA12, TestNode & Node3 are inside. So using Node or TestNode will be trusted. Node3 is expired.

# The directory issuers is filled automaticaly with untrusted, not rejected, certificates from TLS connection. One can move it after in "trusted".
