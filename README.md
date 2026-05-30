# Even Valence Remeshing

The c# implementation is based on my working paper of EVR. 

For more details about the used algorithms, see:

https://hyzok-su.github.io/even-valence-remeshing/EVR.pdf

## Application Example: Weaver Structure using EVR and Simulated Annealing 
(pictures rendered in VRay)

![UUV Mapping](./docs/w1.png) 
![UUV Mapping](./docs/w2.png) 
![UUV Mapping](./docs/w3.png) 
![UUV Mapping](./docs/w4.png) 

## Simulated Annealing for Interlacing Error Reduction

![UUV Mapping](./docs/SA.gif) 

## Dependencies

The isotropic remeshing section uses:

- Plankton (C# half-edge mesh library)
  https://github.com/meshmash/Plankton
- Mesh Machine by Daniel Piker
  https://github.com/Dan-Piker/MeshMachine

## Acknowledgements

Special thanks to the developers for providing the libraries and codes used in this project.
