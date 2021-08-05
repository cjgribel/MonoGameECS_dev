using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

// Example from
// https://www.monogameextended.net/docs/features/entities/entities/

namespace MonoGameECS_dev
{
    public class Expiry
    {
        public Expiry(float timeRemaining)
        {
            TimeRemaining = timeRemaining;
        }

        public float TimeRemaining;
    }
    public class Raindrop
    {
        public Vector2 Velocity;
        public float Size = 3;
    }
    public class ExpirySystem : EntityProcessingSystem
    {
        private ComponentMapper<Expiry> _expiryMapper;

        public ExpirySystem()
            : base(Aspect.All(typeof(Expiry)))
        {
        }

        public override void Initialize(IComponentMapperService mapperService)
        {
            _expiryMapper = mapperService.GetMapper<Expiry>();
        }

        public override void Process(GameTime gameTime, int entityId)
        {
            var expiry = _expiryMapper.Get(entityId);
            expiry.TimeRemaining -= gameTime.GetElapsedSeconds();
            if (expiry.TimeRemaining <= 0)
                DestroyEntity(entityId);
        }
    }
    public class RainfallSystem : EntityUpdateSystem
    {
        private readonly FastRandom _random = new FastRandom();
        private ComponentMapper<Transform2> _transformMapper;
        private ComponentMapper<Raindrop> _raindropMapper;
        private ComponentMapper<Expiry> _expiryMapper;

        private const float MinSpawnDelay = 0.0f;
        private const float MaxSpawnDelay = 0.0f;
        private float _spawnDelay = MaxSpawnDelay;

        public RainfallSystem()
            : base(Aspect.All(typeof(Transform2), typeof(Raindrop)))
        {
        }

        public override void Initialize(IComponentMapperService mapperService)
        {
            _transformMapper = mapperService.GetMapper<Transform2>();
            _raindropMapper = mapperService.GetMapper<Raindrop>();
            _expiryMapper = mapperService.GetMapper<Expiry>();
        }

        public override void Update(GameTime gameTime)
        {
            var elapsedSeconds = gameTime.GetElapsedSeconds();

            foreach (var entityId in ActiveEntities)
            {
                var transform = _transformMapper.Get(entityId);
                var raindrop = _raindropMapper.Get(entityId);

                raindrop.Velocity += new Vector2(0, 500) * elapsedSeconds;
                transform.Position += raindrop.Velocity * elapsedSeconds;

                if (transform.Position.Y >= 480 && !_expiryMapper.Has(entityId))
                {
                    for (var i = 0; i < 3; i++)
                    {
                        var velocity = new Vector2(_random.NextSingle(-100, 100), -raindrop.Velocity.Y * _random.NextSingle(0.1f, 0.2f));
                        var id = CreateRaindrop(transform.Position.SetY(479), velocity, (i + 1) * 0.5f);
                        _expiryMapper.Put(id, new Expiry(1f));
                    }

                    DestroyEntity(entityId);
                }
            }

            _spawnDelay -= gameTime.GetElapsedSeconds();

            if (_spawnDelay <= 0)
            {
                for (var q = 0; q < 50; q++)
                {
                    var position = new Vector2(_random.NextSingle(0, 800), _random.NextSingle(-240, -480));
                    CreateRaindrop(position);
                }
                _spawnDelay = _random.NextSingle(MinSpawnDelay, MaxSpawnDelay);
            }
        }

        private int CreateRaindrop(Vector2 position, Vector2 velocity = default(Vector2), float size = 3)
        {
            var entity = CreateEntity();
            entity.Attach(new Transform2(position));
            entity.Attach(new Raindrop { Velocity = velocity, Size = size });
            return entity.Id;
        }
    }

    public class RenderSystem : EntityDrawSystem
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;

        private ComponentMapper<Transform2> _transformMapper;
        private ComponentMapper<Raindrop> _raindropMapper;

        public RenderSystem(GraphicsDevice graphicsDevice)
            : base(Aspect.All(typeof(Transform2), typeof(Raindrop)))
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public override void Initialize(IComponentMapperService mapperService)
        {
            _transformMapper = mapperService.GetMapper<Transform2>();
            _raindropMapper = mapperService.GetMapper<Raindrop>();
        }

        public override void Draw(GameTime gameTime)
        {
            _graphicsDevice.Clear(Color.DarkBlue * 0.2f);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            foreach (var entity in ActiveEntities)
            {
                var transform = _transformMapper.Get(entity);
                var raindrop = _raindropMapper.Get(entity);

                _spriteBatch.FillRectangle(transform.Position, new Size2(raindrop.Size, raindrop.Size), Color.LightBlue);
            }
            _spriteBatch.End();
        }
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private World world;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
                PreferredBackBufferWidth = 800,
                PreferredBackBufferHeight = 600,
                PreferredBackBufferFormat = SurfaceFormat.Color,
                PreferMultiSampling = false,
                PreferredDepthStencilFormat = DepthFormat.None,
                SynchronizeWithVerticalRetrace = true,
            };

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            world = new WorldBuilder()
                .AddSystem(new RainfallSystem())
                .AddSystem(new ExpirySystem())
                .AddSystem(new RenderSystem(GraphicsDevice))
                .Build();
            Components.Add(world);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            world = new WorldBuilder().Build();
            //    .AddSystem(new PlayerSystem())
            //.AddSystem(new RenderSystem(GraphicsDevice))
            //.Build();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            world.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            world.Draw(gameTime);
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            // TODO: unload the entities
            base.UnloadContent();
        }
    }
}
