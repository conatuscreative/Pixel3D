@ECHO OFF

REM use .gitconfig for this repository
git config --local include.path /.gitconfig

REM first time use
git submodule update --init --recursive

REM switch all submodules to master
git submodule foreach git checkout master

REM update all submodules to use tips of master branches
git submodule foreach git pull origin master