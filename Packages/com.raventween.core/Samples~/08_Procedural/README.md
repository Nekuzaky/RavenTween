# Procedural sample

A small character whose head follows an orbiting target with **Raven Look At**, an antenna that swings with **Raven Spring Chain** as the body hops, and a look-away every few seconds made by tweening the look-at weight.

Create an empty scene, add `ProceduralDemo` to an empty GameObject, and press Play. No other package is needed, Animation Rigging included.

Things to try in Play Mode:

- Select the **Antenna** object and change **Stiffness**, **Damping** and **Gravity** on its Raven Spring Chain.
- Select the **Head** object and change **Max Angle** or **Smooth Time** on its Raven Look At.
- Open **Tools ▸ RavenTween ▸ Monitor** to watch the hop, the orbit and the weight sequence live.
