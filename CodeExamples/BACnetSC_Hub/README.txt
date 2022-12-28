BACnet/SC Hub is ... a questionable (partially respecting the protocol) but working implementation of HUB service, but to experiment PKI.
This application can be used for Wireshark capture of cyphered channel in an uncyphered channel : for programmer only, see code.

For Windows 10 see trunk\Docs\ActivateTLS1.3 On Win10.reg

The process of certificate validation is coded by a no expert (me) and need to be greatly modified.
Please help : fchaxel@free.fr

PKI directory :

# In "certdatabase" directory the file xca.xdb is an XCA database (https://hohnstaedt.de/xca/index.php) with keys & certificates.
The database is not encrypted nor password protected. A real one should be !
All exported certificates are present. P12 files (cert + private Key without password) are in the "p12...." directory.

Certificates tree structure is :

    CA1
      |
      ---- CA11  ------------- CA12
              |                           |
               --- FakeNode        --- Hub
                                            --- Node
                                            --- Node2
                                            --- Node3 (outdated)

and also a tree structure coming from the BACnet/SC Refercence Stack (https://sourceforge.net/projects/bacnet-sc-reference-stack/) :

    Bar_Signing_CA
      |
       ---- TestNode
       ---- TestHub
          
# In "own" directory the Hub application certificate can be found (with private key not password protected).

# In "rejected" you can place certificates not accepted by the application (endUser or CA12 or CA1-CA12 ...). 
  By default Node2 and CA11 are inside. So using FakeNode will be rejected too.

# In "trusted" you can place certificates accepted by the application (endUser or CA12 or CA1-CA12 ...). 
   By default CA12, TestNode & Node3 are inside. So using Node or TestNode will be trusted. Node3 is outdated.

# The directory issuers is filled automaticaly with untrusted certificates from TLS connection. One can move it after in "trusted"
