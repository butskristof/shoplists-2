<script setup lang="ts">

</script>

<template>
  <h1>Welcome to Shoplists</h1>
  <p>Your grocery shopping companion.</p>

  <Toolbar>
    <template #start>
      <Button icon="pi pi-plus" class="mr-2" severity="secondary" text />
      <Button icon="pi pi-print" class="mr-2" severity="secondary" text />
      <Button icon="pi pi-upload" severity="secondary" text />
    </template>

    <template #center>
      <IconField>
        <InputIcon>
          <i class="pi pi-search" />
        </InputIcon>
        <InputText placeholder="Search" />
      </IconField>
    </template>

    <template #end>
      <SplitButton label="Save" />
    </template>
  </Toolbar>

  <div>
    <Button label="Get Started" />
  </div>

  <div class="stepper-card">
    <Stepper class="stepper" value="1">
      <StepList>
        <Step value="1">
          Header I
        </Step>
        <Step value="2">
          Header II
        </Step>
        <Step value="3">
          Header III
        </Step>
      </StepList>
      <StepPanels>
        <StepPanel v-slot="{ activateCallback }" value="1">
          <div class="flex flex-col h-48">
            <div class="border-2 border-dashed border-surface-200 dark:border-surface-700 rounded bg-surface-50 dark:bg-surface-950 flex-auto flex justify-center items-center font-medium">
              Content I
            </div>
          </div>
          <div class="flex pt-6 justify-end">
            <Button label="Next" icon="pi pi-arrow-right" icon-pos="right" @click="activateCallback('2')" />
          </div>
        </StepPanel>
        <StepPanel v-slot="{ activateCallback }" value="2">
          <div class="flex flex-col h-48">
            <div class="border-2 border-dashed border-surface-200 dark:border-surface-700 rounded bg-surface-50 dark:bg-surface-950 flex-auto flex justify-center items-center font-medium">
              Content II
            </div>
          </div>
          <div class="flex pt-6 justify-between">
            <Button label="Back" severity="secondary" icon="pi pi-arrow-left" @click="activateCallback('1')" />
            <Button label="Next" icon="pi pi-arrow-right" icon-pos="right" @click="activateCallback('3')" />
          </div>
        </StepPanel>
        <StepPanel v-slot="{ activateCallback }" value="3">
          <div class="flex flex-col h-48">
            <div class="border-2 border-dashed border-surface-200 dark:border-surface-700 rounded bg-surface-50 dark:bg-surface-950 flex-auto flex justify-center items-center font-medium">
              Content III
            </div>
          </div>
          <div class="pt-6">
            <Button label="Back" severity="secondary" icon="pi pi-arrow-left" @click="activateCallback('2')" />
          </div>
        </StepPanel>
      </StepPanels>
    </Stepper>
  </div>

  <Fieldset class="fieldset" legend="Header">
    <p>
      Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
      Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </p>
  </Fieldset>
</template>

<style scoped>
.stepper-card {
  display: flex;
  justify-content: center;

  .stepper {
    flex-basis: 50rem;
  }
}

.fieldset {
  p {
    margin: 0;
  }
}
</style>
