import { initializeApp } from 'firebase/app';
import {
    getAuth, onAuthStateChanged, signOut, signInWithEmailAndPassword,
    createUserWithEmailAndPassword, signInWithPopup, GoogleAuthProvider, FacebookAuthProvider,
} from 'firebase/auth';

const $=id=>document.getElementById(id);
const guestKey='costa-guest-v1';

function loadGuest() {
    try{return localStorage.getItem(guestKey)==='yes';}catch{return false;}
}
function setGuest(active) {
    try{if(active)localStorage.setItem(guestKey,'yes');else localStorage.removeItem(guestKey);}catch{}
}

export function setupAccount({enter,leave}) {
    const config=window.ZERO_FIREBASE;
    const web=location.protocol==='https:'&&location.hostname!=='appassets.androidplatform.net';
    const available=Boolean(config?.apiKey&&config.authDomain&&config.projectId&&web);
    let auth=null,busy=false,guestActive=loadGuest();
    let profile=null;
    function setProfile(value) {
        if(!value&&!profile)return;
        profile=value;
        if(value)enter(value);
        else leave();
    }
    function status(text){$('account-status').textContent=text;}
    function enableControls(){
        for(const id of ['account-google','account-facebook','account-email'])$(id).disabled=!auth||busy;
        $('account-guest').disabled=busy;
    }
    function openGuest() {
        guestActive=true;setGuest(true);
        setProfile({key:'guest',label:'Convidado'});
    }
    async function authenticate(action) {
        if(!auth||busy)return;
        busy=true;enableControls();status('Conectando…');
        try{
            await action();
            guestActive=false;setGuest(false);
            $('account-password').value='';
            $('email-form').hidden=true;
            $('account-email').setAttribute('aria-expanded','false');
            status('');
        }catch(error){
            const message={
                'auth/invalid-credential':'E-mail ou senha inválidos.',
                'auth/email-already-in-use':'Este e-mail já tem uma conta.',
                'auth/popup-closed-by-user':'Janela de login fechada.',
                'auth/popup-blocked':'Permita a janela de login no navegador.',
                'auth/unauthorized-domain':'Este domínio precisa ser autorizado no projeto de contas.',
                'auth/network-request-failed':'Falha de conexão com o serviço de contas.',
                'auth/weak-password':'Use uma senha de pelo menos 6 caracteres.',
            }[error.code];
            status(message||'Não foi possível entrar. Confira o provedor no serviço de contas.');
        }finally{busy=false;enableControls();}
    }
    if(available)try{
        auth=getAuth(initializeApp(config));
        onAuthStateChanged(auth,user=>{
            if(user)setProfile({key:'firebase:'+user.uid,label:user.displayName||user.email||'Conta'});
            else if(guestActive)openGuest();
            else setProfile(null);
        },()=>status('Serviço de contas indisponível.'));
    }catch{status('Falha ao iniciar o serviço de contas.');}
    if(!auth){
        status('O acesso online ainda não está configurado nesta versão.');
        if(guestActive)openGuest();
    }
    enableControls();
    $('account-guest').onclick=openGuest;
    $('account-email').onclick=()=>{
        const form=$('email-form');
        form.hidden=!form.hidden;
        $('account-email').setAttribute('aria-expanded',String(!form.hidden));
        if(!form.hidden)$('account-address').focus();
    };
    $('account-google').onclick=()=>authenticate(()=>signInWithPopup(auth,new GoogleAuthProvider()));
    $('account-facebook').onclick=()=>authenticate(()=>signInWithPopup(auth,new FacebookAuthProvider()));
    $('email-form').onsubmit=e=>{
        e.preventDefault();
        const email=$('account-address').value.trim(),password=$('account-password').value;
        const fn=e.submitter?.value==='register'?createUserWithEmailAndPassword:signInWithEmailAndPassword;
        authenticate(()=>fn(auth,email,password));
    };
    $('account-exit').onclick=async()=>{
        if(busy)return;
        guestActive=false;setGuest(false);
        if(auth?.currentUser)try{await signOut(auth);}catch{status('Falha ao sair da conta.');return;}
        setProfile(null);
    };
}
