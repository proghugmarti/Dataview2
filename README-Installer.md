# DATAVIEW2

# How To Deploy a Installer:
1- Create the certificate only once so you will able to generate the msix Installer every time you want. 
Use the script: \ScriptsDeployment\CertificateCreationDataView.ps1

You, as a Developer must to:

1.1-Export it by clicking on Windows, Typing "Manage user certificates" and in theCertificates "Node" below  the Personal "Node" click 
in the right Panel on the certificate just generated "Coding MiniRomdas" -> All tasks -> Export: Next x 3 and select the destination 
folder to leave it.

1.2-Include it by clicking on Windows, Typing "Manage Computer Certificates" and in the Certificates "Node" 
below the Trusted Root Certification "Node" right click -> All Tasks -> Import: Next -> (Choose the generated certificate) and click Next
onwards. 

2- The script (DataView2Deployment.ps1) is located here:  
\ScriptsDeployment\DataView2Deployment.ps1

With it you can create the MSIX Installer : DataView2_1.0.0.1_x64.msix
It will be located at this path: \Source\Dcl.MiniRomdas\bin\Release\net7.0-windows10.0.22000.0\win10-x64\AppPackages\
DataView2_1.0.0.1_Test\

3- Make sure to always keep this file (\Source\Repos\dataview2\DataView2\.publish\DataView2Services.zip), in that path to avoid Deployment failures.

Once the installer (MSIX) created place it with the certificate in the folder named: \ScriptsCustomer\,  and copy the folder to the customer machine.

## On the Customer machine:
4- The only script that the customer will have to execute is this: \ScriptsCustomer\Setup.bat



