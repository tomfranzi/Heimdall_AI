# Heimdall AI

<p align="center">
  <img src="Heimdall_AI/Resources/Icone/neurology.svg" alt="Heimdall AI" width="84" />
</p>

<p align="center">
  <b>Surveillance audio intelligente en temps réel</b><br/>
  <sub>Interface moderne, 100% en français, alertes critiques, historique filtrable.</sub>
</p>

---

## ✨ Aperçu

**Heimdall AI** est une application .NET MAUI qui permet de :
- recevoir des détections sonores via MQTT,
- afficher des alertes critiques en plein écran,
- gérer des filtres sonores personnalisés,
- consulter un historique visuel des événements,
- sécuriser l’accès (compte local + biométrie Android).

L’application a été pensée pour un usage mobile avec une interface cohérente, moderne et lisible.

---

## 🧩 Fonctionnalités principales

### 🔔 Alertes intelligentes
- Notifications locales en mode normal et critique.
- Alerte rouge plein écran en mode alerte.
- Sirène critique avec désactivation manuelle.
- Bouton d’urgence qui compose directement le **17**.

### 🎚️ Paramètres automatiques
- Les réglages sont appliqués **sans bouton Enregistrer**.
- Sensibilité micro en pourcentage, convertie au format attendu côté MQTT.
- Catégories sonores activables/désactivables individuellement.

### 🕘 Historique des événements
- Historique persistant entre les lancements.
- Filtrage par type de bruit.
- Défilement vertical des anciennes notifications.
- Style visuel harmonisé avec les paramètres (icônes/couleurs).

### 🔐 Authentification
- Comptes locaux (création, modification, suppression).
- Connexion biométrique Android (empreinte / reconnaissance faciale selon appareil).
- Session locale mémorisée sur le même téléphone.

---

## 🗂️ Persistance des données (BDD fichier)

L’application enregistre ses données localement en JSON (dans le répertoire applicatif) :

- `alertes_db.json` : alerte active + historique
- `auth_db.json` : comptes + dernier utilisateur

Cela permet de retrouver les informations au redémarrage de l’application.

---

## 🏗️ Stack technique

- **.NET 10**
- **.NET MAUI** (Android / iOS / Windows / MacCatalyst)
- **CommunityToolkit.Mvvm**
- **CommunityToolkit.Maui**
- **MQTTnet**

---

## 🚀 Lancer le projet

### Prérequis
- Visual Studio 2026 (ou version compatible .NET MAUI)
- SDK .NET 10
- Workloads MAUI installés
- Broker MQTT accessible

### Android (local)
1. Ouvrir la solution `Heimdall_AI.sln`.
2. Sélectionner la cible `net10.0-android`.
3. Lancer en Debug sur émulateur ou appareil.

### Android (Samsung)
1. Activer `Options développeur` + `Débogage USB`.
2. Installer **Samsung USB Driver**.
3. Vérifier la détection avec `adb devices`.
4. Déployer depuis Visual Studio.

### iOS
Le déploiement iOS nécessite un **Mac avec Xcode**, un certificat et un profil de provisioning.

---

## 🧭 Structure du projet (résumé)

- `Views/` : pages XAML (UI)
- `ViewModels/` : logique de présentation (MVVM)
- `Services/` : MQTT, alertes natives, stockage fichier, authentification
- `Models/` : modèles de données (alertes, etc.)
- `Platforms/Android/` : intégrations Android (service foreground, permissions, activité)

---

## 🎨 Ligne directrice UX

- Application entièrement en **français**
- Design sombre moderne et cohérent
- Barre supérieure uniforme
- Icônes homogènes sur toutes les pages

---

## ⚠️ Notes importantes

- Les alertes en arrière-plan Android utilisent un service foreground.
- Le comportement exact des notifications dépend aussi des réglages système du téléphone (autorisations, batterie, mode économie d’énergie).

---

## 🤝 Contribution

Tom
Alex
Mathys
Ali

Les améliorations UI/UX, la robustesse MQTT et la stabilité multi-plateforme sont les axes prioritaires.

Si vous proposez des changements, privilégiez :
- code lisible,
- cohérence visuelle,
- français intégral dans l’interface.

---

<p align="center"><b>Heimdall AI</b> · Surveillance audio intelligente</p>
